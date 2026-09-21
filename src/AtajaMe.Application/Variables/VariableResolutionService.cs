using System.Globalization;
using System.Text.RegularExpressions;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Enums;

namespace AtajaMe.Application.Variables;

/// <summary>
/// Servicio para el análisis y resolución de variables dinámicas automáticas ({date}, {time}, {datetime})
/// e interactivas ({input:nombre}) en el contenido de los atajos.
/// </summary>
public sealed class VariableResolutionService : IVariableResolutionService
{
    private static readonly Regex AutoVariableRegex = new(
        @"\{(date|time|datetime)\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex InteractiveVariableRegex = new(
        @"\{input:([^}]+)\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly VariableResolutionOptions _options;

    public VariableResolutionService(VariableResolutionOptions? options = null)
    {
        _options = options ?? new VariableResolutionOptions();
    }

    public bool HasVariables(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return false;
        }

        return AutoVariableRegex.IsMatch(content) || InteractiveVariableRegex.IsMatch(content);
    }

    public bool HasInteractiveVariables(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return false;
        }

        return InteractiveVariableRegex.IsMatch(content);
    }

    public IReadOnlyList<DynamicVariable> ExtractVariables(string? content)
    {
        return DynamicVariable.ExtractVariables(content);
    }

    public IReadOnlyList<DynamicVariable> ExtractInteractiveVariables(string? content)
    {
        var all = ExtractVariables(content);
        return all.Where(v => v.Type == VariableType.InteractiveInput).ToList();
    }

    public string ResolveAutomaticVariables(string? content, DateTime? referenceTime = null, CultureInfo? culture = null)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        var effectiveTime = referenceTime ?? DateTime.Now;
        var effectiveCulture = culture ?? CultureInfo.CurrentCulture;

        return AutoVariableRegex.Replace(content, match =>
        {
            var identifier = match.Groups[1].Value.ToLowerInvariant();
            return identifier switch
            {
                "date" => effectiveTime.ToString(_options.DateFormat, effectiveCulture),
                "time" => effectiveTime.ToString(_options.TimeFormat, effectiveCulture),
                "datetime" => effectiveTime.ToString(_options.DateTimeFormat, effectiveCulture),
                _ => match.Value
            };
        });
    }

    public string ResolveInteractiveVariables(string? content, IReadOnlyDictionary<string, string>? userInputs)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        var inputDict = userInputs != null
            ? new Dictionary<string, string>(userInputs, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return InteractiveVariableRegex.Replace(content, match =>
        {
            var param = match.Groups[1].Value.Trim();
            string name;
            string? defaultValue = null;

            var pipeIndex = param.IndexOf('|');
            if (pipeIndex >= 0)
            {
                name = param[..pipeIndex].Trim();
                defaultValue = param[(pipeIndex + 1)..].Trim();
            }
            else
            {
                name = param;
            }

            if (inputDict.TryGetValue(name, out var value) && !string.IsNullOrEmpty(value))
            {
                return value;
            }

            return defaultValue ?? string.Empty;
        });
    }

    public string ResolveAll(
        string? content,
        IReadOnlyDictionary<string, string>? userInputs = null,
        DateTime? referenceTime = null,
        CultureInfo? culture = null)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        var resolved = ResolveAutomaticVariables(content, referenceTime, culture);

        if (userInputs != null)
        {
            resolved = ResolveInteractiveVariables(resolved, userInputs);
        }

        return resolved;
    }
}
