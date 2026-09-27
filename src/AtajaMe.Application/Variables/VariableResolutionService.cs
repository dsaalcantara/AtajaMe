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
        @"\{(date|fecha|time|hora|datetime|fechahora|cursor|cw|semana|week)(?::([^}]+))?\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex InteractiveVariableRegex = new(
        @"\{(?:input:([^}]+)|(?!(?:date|fecha|time|hora|datetime|fechahora|cursor|cw|semana|week)(?:[:\}]))([^}:|]+(?:\|[^}]+)?))\}",
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
            var customFormat = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;

            return identifier switch
            {
                "date" or "fecha" => FormatDate(effectiveTime, customFormat, effectiveCulture),
                "time" or "hora" => FormatTime(effectiveTime, customFormat, effectiveCulture),
                "datetime" or "fechahora" => FormatDateTime(effectiveTime, customFormat, effectiveCulture),
                "cw" or "semana" or "week" => FormatCalendarWeek(effectiveTime, customFormat),
                "cursor" => string.Empty, // El cursor se trata en inyección
                _ => match.Value
            };
        });
    }

    private static string FormatCalendarWeek(DateTime time, string? format)
    {
        int weekOffset = 0;
        bool padZero = false;

        if (!string.IsNullOrWhiteSpace(format))
        {
            var trimmed = format.Trim().ToLowerInvariant();

            if (trimmed.Contains("00") || trimmed.EndsWith(":00"))
            {
                padZero = true;
            }

            if (trimmed.Contains("proxima") || trimmed.Contains("próxima") || trimmed.Contains("siguiente") || trimmed.Contains("next") || trimmed.StartsWith("+1") || trimmed.EndsWith("+1") || trimmed == "p")
            {
                weekOffset = 1;
            }
            else if (trimmed.Contains("anterior") || trimmed.Contains("pasada") || trimmed.Contains("prev") || trimmed.Contains("last") || trimmed.StartsWith("-1") || trimmed.EndsWith("-1") || trimmed == "a")
            {
                weekOffset = -1;
            }
            else
            {
                var match = Regex.Match(trimmed, @"([+-]?\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var parsedOffset))
                {
                    weekOffset = parsedOffset;
                }
            }
        }

        var targetDate = time.AddDays(weekOffset * 7);
        var weekNumber = ISOWeek.GetWeekOfYear(targetDate);

        return padZero ? weekNumber.ToString("00") : weekNumber.ToString();
    }

    private string FormatDate(DateTime time, string? customFormat, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(customFormat))
        {
            return time.ToString(_options.DateFormat, culture);
        }

        try
        {
            var normalized = NormalizeDateFormat(customFormat);
            return time.ToString(normalized, culture);
        }
        catch
        {
            return time.ToString(_options.DateFormat, culture);
        }
    }

    private string FormatTime(DateTime time, string? customFormat, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(customFormat))
        {
            return time.ToString(_options.TimeFormat, culture);
        }

        try
        {
            var normalized = NormalizeTimeFormat(customFormat);
            return time.ToString(normalized, culture);
        }
        catch
        {
            return time.ToString(_options.TimeFormat, culture);
        }
    }

    private string FormatDateTime(DateTime time, string? customFormat, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(customFormat))
        {
            return time.ToString(_options.DateTimeFormat, culture);
        }

        try
        {
            var normalized = NormalizeDateTimeFormat(customFormat);
            return time.ToString(normalized, culture);
        }
        catch
        {
            return time.ToString(_options.DateTimeFormat, culture);
        }
    }

    public static string NormalizeDateFormat(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return format;
        }

        // Si contiene texto literal entre comillas simples, preservamos los literales
        var parts = format.Split('\'');
        for (int i = 0; i < parts.Length; i += 2)
        {
            string part = parts[i];
            // Año: aaaa -> yyyy, aa -> yy, a -> y
            part = Regex.Replace(part, "aaaa", "yyyy", RegexOptions.IgnoreCase);
            part = Regex.Replace(part, "aa", "yy", RegexOptions.IgnoreCase);
            part = Regex.Replace(part, "a", "y", RegexOptions.IgnoreCase);

            // Mes: en contexto de fecha, cualquier m/mm representa mes (no minutos)
            part = Regex.Replace(part, "mmmm", "MMMM", RegexOptions.IgnoreCase);
            part = Regex.Replace(part, "mmm", "MMM", RegexOptions.IgnoreCase);
            part = Regex.Replace(part, "mm", "MM", RegexOptions.IgnoreCase);
            part = Regex.Replace(part, "m", "M", RegexOptions.IgnoreCase);

            // Día: DD -> dd, D -> d
            part = Regex.Replace(part, "DDDD", "dddd");
            part = Regex.Replace(part, "DDD", "ddd");
            part = Regex.Replace(part, "DD", "dd");
            part = Regex.Replace(part, "D", "d");

            parts[i] = part;
        }

        return string.Join("'", parts);
    }

    public static string NormalizeTimeFormat(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return format;
        }

        var trimmed = format.Trim();
        if (!trimmed.Contains("tt", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = Regex.Replace(trimmed, @"(?<![a-zA-Z])hh(?![a-zA-Z])", "HH");
        }
        return trimmed;
    }

    public static string NormalizeDateTimeFormat(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return format;
        }

        var trimmed = format.Trim();
        var parts = trimmed.Split(new[] { ' ', 'T' }, 2);
        if (parts.Length == 2)
        {
            var datePart = NormalizeDateFormat(parts[0]);
            var timePart = NormalizeTimeFormat(parts[1]);
            char sep = trimmed[parts[0].Length];
            return $"{datePart}{sep}{timePart}";
        }

        return NormalizeDateFormat(trimmed);
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
            var param = !string.IsNullOrEmpty(match.Groups[1].Value)
                ? match.Groups[1].Value
                : match.Groups[2].Value;

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
                name = param.Trim();
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
