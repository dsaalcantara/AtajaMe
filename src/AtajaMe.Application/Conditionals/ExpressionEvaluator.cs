using System.Globalization;
using System.Text.RegularExpressions;

namespace AtajaMe.Application.Conditionals;

/// <summary>
/// Evalúa expresiones booleanas en condicionales ({si hora &lt; 12 &amp;&amp; dia_semana &lt;= 5}).
/// </summary>
public static class ExpressionEvaluator
{
    private static readonly Regex ComparisonOperatorRegex = new(
        @"\s*(<=|>=|==|!=|<>|=|<|>)\s*",
        RegexOptions.Compiled);

    /// <summary>
    /// Evalúa la expresión booleana contra el contexto proporcionado.
    /// Devuelve false de forma segura si la sintaxis no es válida.
    /// </summary>
    public static bool Evaluate(string expression, ConditionContext context)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        var trimmed = expression.Trim();

        // 1. Manejo de conectores lógicos de menor precedencia: '||', ' or ', ' o '
        var orParts = SplitLogical(trimmed, new[] { "||", " or ", " o " });
        if (orParts.Count > 1)
        {
            foreach (var part in orParts)
            {
                if (Evaluate(part, context))
                {
                    return true;
                }
            }
            return false;
        }

        // 2. Manejo de conectores lógicos: '&&', ' and ', ' y '
        var andParts = SplitLogical(trimmed, new[] { "&&", " and ", " y " });
        if (andParts.Count > 1)
        {
            foreach (var part in andParts)
            {
                if (!Evaluate(part, context))
                {
                    return false;
                }
            }
            return true;
        }

        // 3. Manejo de negación: '!exp' o 'no exp' o 'not exp'
        if (trimmed.StartsWith("!") || trimmed.StartsWith("no ", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("not ", StringComparison.OrdinalIgnoreCase))
        {
            string inner = trimmed.StartsWith("!") ? trimmed[1..] : (trimmed.StartsWith("no ", StringComparison.OrdinalIgnoreCase) ? trimmed[3..] : trimmed[4..]);
            return !Evaluate(inner, context);
        }

        // 4. Evaluación de comparación binaria simple: <izquierda> <operador> <derecha>
        return EvaluateComparison(trimmed, context);
    }

    private static bool EvaluateComparison(string expression, ConditionContext context)
    {
        var match = ComparisonOperatorRegex.Match(expression);
        if (!match.Success)
        {
            // Valor booleano solitario o variable que evalúa a verdad si no está vacía / es distinto de cero
            return EvaluateSingleValue(expression, context);
        }

        var leftRaw = expression[..match.Index].Trim();
        var op = match.Groups[1].Value;
        var rightRaw = expression[(match.Index + match.Length)..].Trim();

        var leftVal = ResolveValue(leftRaw, context);
        var rightVal = ResolveValue(rightRaw, context);

        return CompareValues(leftVal, op, rightVal);
    }

    private static bool EvaluateSingleValue(string token, ConditionContext context)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var trimmed = token.Trim();

        // Si es literal booleano
        if (trimmed.Equals("true", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("verdadero", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (trimmed.Equals("false", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("falso", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Si es una variable del contexto
        if (context.TryResolveValue(trimmed, out var val))
        {
            if (val is bool b)
            {
                return b;
            }
            if (val is double d)
            {
                return d != 0;
            }
            if (val is int i)
            {
                return i != 0;
            }
            var s = val?.ToString()?.Trim() ?? string.Empty;
            if (s.Equals("true", StringComparison.OrdinalIgnoreCase) || s.Equals("verdadero", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (s.Equals("false", StringComparison.OrdinalIgnoreCase) || s.Equals("falso", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return !string.IsNullOrEmpty(s);
        }

        return false;
    }

    private static object? ResolveValue(string token, ConditionContext context)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        var trimmed = token.Trim();

        // 1. Literal entre comillas simples o dobles
        if ((trimmed.StartsWith("'") && trimmed.EndsWith("'") && trimmed.Length >= 2) ||
            (trimmed.StartsWith("\"") && trimmed.EndsWith("\"") && trimmed.Length >= 2))
        {
            return trimmed[1..^1];
        }

        // 2. Literal booleano
        if (trimmed.Equals("true", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("verdadero", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (trimmed.Equals("false", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("falso", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // 3. Literal numérico
        if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
        {
            return num;
        }

        // 4. Intentar resolver como variable del contexto
        if (context.TryResolveValue(trimmed, out var contextValue))
        {
            return contextValue;
        }

        // 5. Cadena literal sin comillas
        return trimmed;
    }

    private static bool CompareValues(object? left, string op, object? right)
    {
        // Intentar comparación numérica si ambos pueden convertirse a double
        if (TryConvertToDouble(left, out var leftNum) && TryConvertToDouble(right, out var rightNum))
        {
            return op switch
            {
                "==" or "=" => Math.Abs(leftNum - rightNum) < 0.0000001,
                "!=" or "<>" => Math.Abs(leftNum - rightNum) >= 0.0000001,
                "<" => leftNum < rightNum,
                "<=" => leftNum <= rightNum,
                ">" => leftNum > rightNum,
                ">=" => leftNum >= rightNum,
                _ => false
            };
        }

        // Comparación de cadenas de texto (insensible a mayúsculas/minúsculas)
        var leftStr = left?.ToString() ?? string.Empty;
        var rightStr = right?.ToString() ?? string.Empty;

        return op switch
        {
            "==" or "=" => string.Equals(leftStr, rightStr, StringComparison.OrdinalIgnoreCase),
            "!=" or "<>" => !string.Equals(leftStr, rightStr, StringComparison.OrdinalIgnoreCase),
            "<" => string.Compare(leftStr, rightStr, StringComparison.OrdinalIgnoreCase) < 0,
            "<=" => string.Compare(leftStr, rightStr, StringComparison.OrdinalIgnoreCase) <= 0,
            ">" => string.Compare(leftStr, rightStr, StringComparison.OrdinalIgnoreCase) > 0,
            ">=" => string.Compare(leftStr, rightStr, StringComparison.OrdinalIgnoreCase) >= 0,
            _ => false
        };
    }

    private static bool TryConvertToDouble(object? value, out double number)
    {
        if (value is int i)
        {
            number = i;
            return true;
        }
        if (value is double d)
        {
            number = d;
            return true;
        }
        if (value is float f)
        {
            number = f;
            return true;
        }
        if (value is long l)
        {
            number = l;
            return true;
        }

        if (value is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            number = parsed;
            return true;
        }

        number = 0;
        return false;
    }

    private static List<string> SplitLogical(string expression, string[] delimiters)
    {
        var result = new List<string>();
        int start = 0;

        for (int i = 0; i < expression.Length; i++)
        {
            // Ignorar delimitadores si estamos dentro de comillas
            if (expression[i] is '\'' or '"')
            {
                char quote = expression[i];
                i++;
                while (i < expression.Length && expression[i] != quote)
                {
                    i++;
                }
                continue;
            }

            foreach (var delimiter in delimiters)
            {
                if (i + delimiter.Length <= expression.Length &&
                    expression.Substring(i, delimiter.Length).Equals(delimiter, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(expression[start..i].Trim());
                    start = i + delimiter.Length;
                    i = start - 1;
                    break;
                }
            }
        }

        if (start < expression.Length)
        {
            result.Add(expression[start..].Trim());
        }

        return result;
    }
}
