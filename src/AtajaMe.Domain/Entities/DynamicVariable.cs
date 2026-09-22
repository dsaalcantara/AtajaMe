using System.Text.RegularExpressions;
using AtajaMe.Domain.Enums;

namespace AtajaMe.Domain.Entities;

/// <summary>
/// Representa una variable dinámica extraída de una plantilla de contenido.
/// </summary>
public sealed class DynamicVariable
{
    private static readonly Regex VariableRegex = new(
        @"\{(date|fecha|time|hora|datetime|fechahora|cursor|input:([^}]+)|([^}:|]+(?:\|[^}]+)?))\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public VariableType Type { get; }
    public string RawTag { get; }
    public string Name { get; }
    public string? DefaultValue { get; }

    public DynamicVariable(VariableType type, string rawTag, string name, string? defaultValue = null)
    {
        Type = type;
        RawTag = rawTag ?? throw new ArgumentNullException(nameof(rawTag));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// Extrae todas las variables dinámicas presentes en el contenido de un atajo.
    /// </summary>
    public static IReadOnlyList<DynamicVariable> ExtractVariables(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<DynamicVariable>();
        }

        var matches = VariableRegex.Matches(content);
        var variables = new List<DynamicVariable>(matches.Count);

        foreach (Match match in matches)
        {
            var rawTag = match.Value;
            var identifier = match.Groups[1].Value.ToLowerInvariant();

            if (identifier is "date" or "fecha")
            {
                variables.Add(new DynamicVariable(VariableType.SystemDate, rawTag, "Fecha"));
            }
            else if (identifier is "time" or "hora")
            {
                variables.Add(new DynamicVariable(VariableType.SystemTime, rawTag, "Hora"));
            }
            else if (identifier is "datetime" or "fechahora")
            {
                variables.Add(new DynamicVariable(VariableType.SystemDateTime, rawTag, "Fecha y Hora"));
            }
            else if (identifier is "cursor")
            {
                variables.Add(new DynamicVariable(VariableType.CursorPosition, rawTag, "Cursor"));
            }
            else
            {
                var inputParam = identifier.StartsWith("input:")
                    ? match.Groups[2].Value.Trim()
                    : match.Groups[3].Value.Trim();

                string name;
                string? defaultValue = null;

                var pipeIndex = inputParam.IndexOf('|');
                if (pipeIndex >= 0)
                {
                    name = inputParam[..pipeIndex].Trim();
                    defaultValue = inputParam[(pipeIndex + 1)..].Trim();
                }
                else
                {
                    name = inputParam;
                }

                variables.Add(new DynamicVariable(VariableType.InteractiveInput, rawTag, name, defaultValue));
            }
        }

        return variables;
    }
}
