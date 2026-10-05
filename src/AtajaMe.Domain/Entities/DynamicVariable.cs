using System.Text.RegularExpressions;
using AtajaMe.Domain.Enums;

namespace AtajaMe.Domain.Entities;

/// <summary>
/// Representa una variable dinámica extraída de una plantilla de contenido.
/// </summary>
public sealed class DynamicVariable
{
    private static readonly Regex VariableRegex = new(
        @"\{(date|fecha|time|hora|datetime|fechahora|cursor|cw|semana|week|mes|month|dia|day|año|ano|year|trimestre|quarter|q|cuatrimestre|fourmonth|c)(?::([^}]+))?\}|\{(?:input:([^}]+)|(?!(?:date|fecha|time|hora|datetime|fechahora|cursor|cw|semana|week|mes|month|dia|day|año|ano|year|trimestre|quarter|q|cuatrimestre|fourmonth|c|si\s+|if\s+|sino|sinosi|else|elif|/si|/if|fin\s*si|finsi|endif)(?:[:\s\}]))([^}:|]+(?:\|[^}]+)?))\}",
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
                var format = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
                var displayName = string.IsNullOrEmpty(format) ? "Fecha" : $"Fecha ({format})";
                variables.Add(new DynamicVariable(VariableType.SystemDate, rawTag, displayName));
            }
            else if (identifier is "time" or "hora")
            {
                var format = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
                var displayName = string.IsNullOrEmpty(format) ? "Hora" : $"Hora ({format})";
                variables.Add(new DynamicVariable(VariableType.SystemTime, rawTag, displayName));
            }
            else if (identifier is "datetime" or "fechahora")
            {
                var format = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
                var displayName = string.IsNullOrEmpty(format) ? "Fecha y Hora" : $"Fecha y Hora ({format})";
                variables.Add(new DynamicVariable(VariableType.SystemDateTime, rawTag, displayName));
            }
            else if (identifier is "cw" or "semana" or "week")
            {
                var format = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
                var displayName = string.IsNullOrEmpty(format) ? "Semana" : $"Semana ({format})";
                variables.Add(new DynamicVariable(VariableType.CalendarWeek, rawTag, displayName));
            }
            else if (identifier is "mes" or "month")
            {
                var format = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
                var displayName = string.IsNullOrEmpty(format) ? "Mes" : $"Mes ({format})";
                variables.Add(new DynamicVariable(VariableType.Month, rawTag, displayName));
            }
            else if (identifier is "dia" or "day")
            {
                var format = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
                var displayName = string.IsNullOrEmpty(format) ? "Día" : $"Día ({format})";
                variables.Add(new DynamicVariable(VariableType.Day, rawTag, displayName));
            }
            else if (identifier is "año" or "ano" or "year")
            {
                var format = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
                var displayName = string.IsNullOrEmpty(format) ? "Año" : $"Año ({format})";
                variables.Add(new DynamicVariable(VariableType.Year, rawTag, displayName));
            }
            else if (identifier is "trimestre" or "quarter" or "q")
            {
                var format = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
                var displayName = string.IsNullOrEmpty(format) ? "Trimestre" : $"Trimestre ({format})";
                variables.Add(new DynamicVariable(VariableType.Quarter, rawTag, displayName));
            }
            else if (identifier is "cuatrimestre" or "fourmonth" or "c")
            {
                var format = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
                var displayName = string.IsNullOrEmpty(format) ? "Cuatrimestre" : $"Cuatrimestre ({format})";
                variables.Add(new DynamicVariable(VariableType.FourMonth, rawTag, displayName));
            }
            else if (identifier is "cursor")
            {
                variables.Add(new DynamicVariable(VariableType.CursorPosition, rawTag, "Cursor"));
            }
            else
            {
                var inputParam = !string.IsNullOrEmpty(match.Groups[3].Value)
                    ? match.Groups[3].Value.Trim()
                    : match.Groups[4].Value.Trim();

                if (string.IsNullOrWhiteSpace(inputParam) ||
                    inputParam.StartsWith("/") ||
                    inputParam.StartsWith("si ", StringComparison.OrdinalIgnoreCase) ||
                    inputParam.StartsWith("if ", StringComparison.OrdinalIgnoreCase) ||
                    inputParam.Equals("sino", StringComparison.OrdinalIgnoreCase) ||
                    inputParam.Equals("else", StringComparison.OrdinalIgnoreCase) ||
                    inputParam.StartsWith("sino si", StringComparison.OrdinalIgnoreCase) ||
                    inputParam.StartsWith("sinosi", StringComparison.OrdinalIgnoreCase) ||
                    inputParam.StartsWith("else if", StringComparison.OrdinalIgnoreCase) ||
                    inputParam.StartsWith("elif", StringComparison.OrdinalIgnoreCase) ||
                    inputParam.Equals("endif", StringComparison.OrdinalIgnoreCase) ||
                    inputParam.Equals("finsi", StringComparison.OrdinalIgnoreCase) ||
                    inputParam.Equals("fin si", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

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
