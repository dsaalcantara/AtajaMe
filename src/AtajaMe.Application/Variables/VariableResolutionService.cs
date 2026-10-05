using System.Globalization;
using System.Text.RegularExpressions;
using AtajaMe.Application.Conditionals;
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
        @"\{(date|fecha|time|hora|datetime|fechahora|cursor|cw|semana|week|mes|month|dia|day|año|ano|year|trimestre|quarter|q|cuatrimestre|fourmonth|c)(?::([^}]+))?\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex InteractiveVariableRegex = new(
        @"\{(?:input:([^}]+)|(?!(?:date|fecha|time|hora|datetime|fechahora|cursor|cw|semana|week|mes|month|dia|day|año|ano|year|trimestre|quarter|q|cuatrimestre|fourmonth|c|si\s+|if\s+|sino|sinosi|else|elif|/si|/if|fin\s*si|finsi|endif)(?:[:\s\}]))([^}:|]+(?:\|[^}]+)?))\}",
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

        return AutoVariableRegex.IsMatch(content) ||
               InteractiveVariableRegex.IsMatch(content) ||
               ConditionalBlockParser.HasConditionals(content);
    }

    public bool HasInteractiveVariables(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return false;
        }

        return ExtractInteractiveVariables(content).Count > 0;
    }

    public IReadOnlyList<DynamicVariable> ExtractVariables(string? content)
    {
        return DynamicVariable.ExtractVariables(content);
    }

    public IReadOnlyList<DynamicVariable> ExtractInteractiveVariables(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return Array.Empty<DynamicVariable>();
        }

        var textToAnalyze = content;
        if (ConditionalBlockParser.HasConditionals(textToAnalyze))
        {
            var timeContext = new ConditionContext(DateTime.Now, null, CultureInfo.CurrentCulture);
            textToAnalyze = ConditionalBlockParser.ProcessConditionals(textToAnalyze, timeContext);
        }

        var all = ExtractVariables(textToAnalyze);
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

        var textToProcess = content;
        if (ConditionalBlockParser.HasConditionals(textToProcess))
        {
            var condContext = new ConditionContext(effectiveTime, null, effectiveCulture);
            textToProcess = ConditionalBlockParser.ProcessConditionals(textToProcess, condContext);
        }

        return AutoVariableRegex.Replace(textToProcess, match =>
        {
            var identifier = match.Groups[1].Value.ToLowerInvariant();
            var customFormat = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;

            return identifier switch
            {
                "date" or "fecha" => FormatDate(effectiveTime, customFormat, effectiveCulture),
                "time" or "hora" => FormatTime(effectiveTime, customFormat, effectiveCulture),
                "datetime" or "fechahora" => FormatDateTime(effectiveTime, customFormat, effectiveCulture),
                "cw" or "semana" or "week" => FormatCalendarWeek(effectiveTime, customFormat),
                "mes" or "month" => FormatMonth(effectiveTime, customFormat, effectiveCulture),
                "dia" or "day" => FormatDay(effectiveTime, customFormat, effectiveCulture),
                "año" or "ano" or "year" => FormatYear(effectiveTime, customFormat, effectiveCulture),
                "trimestre" or "quarter" or "q" => FormatQuarter(effectiveTime, customFormat, identifier),
                "cuatrimestre" or "fourmonth" or "c" => FormatFourMonth(effectiveTime, customFormat),
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

    private static (int offset, List<string> tokens) ParsePeriodTokens(string? format)
    {
        var tokens = new List<string>();
        int offset = 0;

        if (string.IsNullOrWhiteSpace(format))
        {
            return (0, tokens);
        }

        var parts = format.Split(new[] { ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var rawPart in parts)
        {
            var part = rawPart.Trim();
            if (string.IsNullOrEmpty(part))
            {
                continue;
            }

            var lower = part.ToLowerInvariant();
            if (lower is "proximo" or "próxima" or "proxima" or "próximo" or "siguiente" or "next" or "mañana" or "manana" or "tomorrow")
            {
                offset = 1;
            }
            else if (lower is "anterior" or "pasada" or "pasado" or "prev" or "last" or "ayer" or "yesterday")
            {
                offset = -1;
            }
            else if (lower is "hoy" or "today")
            {
                offset = 0;
            }
            else if (Regex.IsMatch(part, @"^[+-]\d+$") && int.TryParse(part, out var parsedNumber))
            {
                offset = parsedNumber;
            }
            else
            {
                tokens.Add(part);
            }
        }

        return (offset, tokens);
    }

    private static string FormatMonth(DateTime time, string? format, CultureInfo culture)
    {
        var (offset, tokens) = ParsePeriodTokens(format);
        var targetDate = time.AddMonths(offset);

        if (tokens.Count == 0)
        {
            return targetDate.ToString("MM", culture);
        }

        var token = tokens[0];
        var lower = token.ToLowerInvariant();

        if (lower is "nombre" or "name")
        {
            var monthName = targetDate.ToString("MMMM", culture);
            if (token == "Nombre" || token == "Name")
            {
                return ToTitleCase(monthName, culture);
            }
            if (token is "NOMBRE" or "MAYUS" or "UPPER")
            {
                return monthName.ToUpper(culture);
            }
            return monthName.ToLower(culture);
        }

        if (lower is "mayus" or "mayusculas" or "upper")
        {
            return targetDate.ToString("MMMM", culture).ToUpper(culture);
        }

        if (lower is "corto" or "short" or "abr" or "abrev")
        {
            var shortName = targetDate.ToString("MMM", culture).TrimEnd('.');
            if (char.IsUpper(token[0]))
            {
                return ToTitleCase(shortName, culture);
            }
            return shortName.ToLower(culture);
        }

        if (lower is "0" or "num" or "numero" or "digito")
        {
            return targetDate.Month.ToString();
        }

        if (lower is "00" or "mm")
        {
            return targetDate.ToString("MM", culture);
        }

        return targetDate.ToString(token, culture);
    }

    private static string FormatDay(DateTime time, string? format, CultureInfo culture)
    {
        var (offset, tokens) = ParsePeriodTokens(format);
        var targetDate = time.AddDays(offset);

        if (tokens.Count == 0)
        {
            return targetDate.ToString("dd", culture);
        }

        var token = tokens[0];
        var lower = token.ToLowerInvariant();

        if (lower is "nombre" or "name")
        {
            var dayName = targetDate.ToString("dddd", culture);
            if (token == "Nombre" || token == "Name")
            {
                return ToTitleCase(dayName, culture);
            }
            if (token is "NOMBRE" or "MAYUS" or "UPPER")
            {
                return dayName.ToUpper(culture);
            }
            return dayName.ToLower(culture);
        }

        if (lower is "mayus" or "mayusculas" or "upper")
        {
            return targetDate.ToString("dddd", culture).ToUpper(culture);
        }

        if (lower is "corto" or "short" or "abr")
        {
            var shortName = targetDate.ToString("ddd", culture).TrimEnd('.');
            if (char.IsUpper(token[0]))
            {
                return ToTitleCase(shortName, culture);
            }
            return shortName.ToLower(culture);
        }

        if (lower is "0" or "num" or "numero" or "digito")
        {
            return targetDate.Day.ToString();
        }

        if (lower is "año" or "ano" or "year" or "doy")
        {
            return targetDate.DayOfYear.ToString();
        }

        if (lower is "00" or "dd")
        {
            return targetDate.ToString("dd", culture);
        }

        return targetDate.ToString(token, culture);
    }

    private static string FormatYear(DateTime time, string? format, CultureInfo culture)
    {
        var (offset, tokens) = ParsePeriodTokens(format);
        var targetDate = time.AddYears(offset);

        if (tokens.Count == 0)
        {
            return targetDate.ToString("yyyy", culture);
        }

        var lower = tokens[0].ToLowerInvariant();
        if (lower is "corto" or "short" or "aa" or "yy" or "2")
        {
            return targetDate.ToString("yy", culture);
        }

        return targetDate.ToString("yyyy", culture);
    }

    private static string FormatQuarter(DateTime time, string? format, string identifier)
    {
        var (offset, tokens) = ParsePeriodTokens(format);
        var targetDate = time.AddMonths(offset * 3);
        int quarter = (targetDate.Month - 1) / 3 + 1;

        bool useQ = identifier is "quarter" or "q";
        bool onlyNum = false;
        bool useRoman = false;

        foreach (var token in tokens)
        {
            var lower = token.ToLowerInvariant();
            if (lower is "q")
            {
                useQ = true;
            }
            else if (lower is "t")
            {
                useQ = false;
            }
            else if (lower is "num" or "numero" or "0")
            {
                onlyNum = true;
            }
            else if (lower is "romano" or "roman")
            {
                useRoman = true;
            }
        }

        if (onlyNum)
        {
            return quarter.ToString();
        }

        if (useRoman)
        {
            return quarter switch
            {
                1 => "I",
                2 => "II",
                3 => "III",
                4 => "IV",
                _ => quarter.ToString()
            };
        }

        return useQ ? $"Q{quarter}" : $"T{quarter}";
    }

    private static string FormatFourMonth(DateTime time, string? format)
    {
        var (offset, tokens) = ParsePeriodTokens(format);
        var targetDate = time.AddMonths(offset * 4);
        int fourMonth = (targetDate.Month - 1) / 4 + 1;

        bool onlyNum = false;
        bool useRoman = false;

        foreach (var token in tokens)
        {
            var lower = token.ToLowerInvariant();
            if (lower is "num" or "numero" or "0")
            {
                onlyNum = true;
            }
            else if (lower is "romano" or "roman")
            {
                useRoman = true;
            }
        }

        if (onlyNum)
        {
            return fourMonth.ToString();
        }

        if (useRoman)
        {
            return fourMonth switch
            {
                1 => "I",
                2 => "II",
                3 => "III",
                _ => fourMonth.ToString()
            };
        }

        return $"C{fourMonth}";
    }

    private static string ToTitleCase(string text, CultureInfo culture)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return char.ToUpper(text[0], culture) + text[1..].ToLower(culture);
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

        var effectiveTime = referenceTime ?? DateTime.Now;
        var effectiveCulture = culture ?? CultureInfo.CurrentCulture;

        var textToProcess = content;
        if (ConditionalBlockParser.HasConditionals(textToProcess))
        {
            var condContext = new ConditionContext(effectiveTime, userInputs, effectiveCulture);
            textToProcess = ConditionalBlockParser.ProcessConditionals(textToProcess, condContext);
        }

        var resolved = ResolveAutomaticVariables(textToProcess, effectiveTime, effectiveCulture);

        if (userInputs != null)
        {
            resolved = ResolveInteractiveVariables(resolved, userInputs);
        }

        return resolved;
    }
}
