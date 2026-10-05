using System.Globalization;

namespace AtajaMe.Application.Conditionals;

/// <summary>
/// Contexto de evaluación para expresiones condicionales ({si ...} / {if ...}).
/// Proporciona acceso a variables temporales del sistema y valores introducidos por el usuario.
/// </summary>
public sealed class ConditionContext
{
    public DateTime ReferenceTime { get; }
    public CultureInfo Culture { get; }
    public IReadOnlyDictionary<string, string> UserInputs { get; }

    public ConditionContext(
        DateTime? referenceTime = null,
        IReadOnlyDictionary<string, string>? userInputs = null,
        CultureInfo? culture = null)
    {
        ReferenceTime = referenceTime ?? DateTime.Now;
        Culture = culture ?? CultureInfo.CurrentCulture;
        UserInputs = userInputs != null
            ? new Dictionary<string, string>(userInputs, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Intenta resolver el valor de un identificador en el contexto.
    /// Soporta variables temporales de sistema e inputs del usuario.
    /// </summary>
    public bool TryResolveValue(string identifier, out object? value)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            value = null;
            return false;
        }

        var key = identifier.Trim();
        var lower = key.ToLowerInvariant();

        // 1. Identificadores temporales del sistema
        switch (lower)
        {
            case "hora" or "hour" or "hours":
                value = ReferenceTime.Hour;
                return true;

            case "minuto" or "minutos" or "minute" or "minutes":
                value = ReferenceTime.Minute;
                return true;

            case "segundo" or "segundos" or "second" or "seconds":
                value = ReferenceTime.Second;
                return true;

            case "dia" or "day":
                value = ReferenceTime.Day;
                return true;

            case "mes" or "month":
                value = ReferenceTime.Month;
                return true;

            case "año" or "ano" or "year":
                value = ReferenceTime.Year;
                return true;

            case "dia_semana" or "diasemana" or "weekday" or "dayofweek":
                // 1=Lunes .. 7=Domingo (convención ISO / europea estándar)
                int isoDayOfWeek = ReferenceTime.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)ReferenceTime.DayOfWeek;
                value = isoDayOfWeek;
                return true;

            case "trimestre" or "quarter":
                value = ((ReferenceTime.Month - 1) / 3) + 1;
                return true;

            case "cuatrimestre" or "fourmonth":
                value = ((ReferenceTime.Month - 1) / 4) + 1;
                return true;
        }

        // 2. Prefijo explícito de input: "input:nombre" o "input.nombre"
        if (lower.StartsWith("input:") || lower.StartsWith("input."))
        {
            var inputKey = key[6..].Trim();
            if (UserInputs.TryGetValue(inputKey, out var inputValue))
            {
                value = inputValue;
                return true;
            }
            value = string.Empty;
            return true;
        }

        // 3. Variables directas introducidas por el usuario
        if (UserInputs.TryGetValue(key, out var directValue))
        {
            value = directValue;
            return true;
        }

        value = null;
        return false;
    }
}
