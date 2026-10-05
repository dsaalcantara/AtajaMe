using System.Text;
using System.Text.RegularExpressions;

namespace AtajaMe.Application.Conditionals;

/// <summary>
/// Parser de bloques condicionales con soporte para anidación ({si ...}{sino si ...}{sino}{/si}).
/// </summary>
public static class ConditionalBlockParser
{
    private static readonly Regex IfOpenRegex = new(
        @"^\{(?:si|if)\s+(.+)\}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ElifRegex = new(
        @"^\{(?:sino\s*si|sinosi|else\s*if|elif)\s+(.+)\}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ElseRegex = new(
        @"^\{(?:sino|else)\}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex EndIfRegex = new(
        @"^\{(?:/si|/if|fin\s*si|finsi|endif)\}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TagFinderRegex = new(
        @"\{(?:(?:si|if)\s+[^\}]+|sino\s*si\s+[^\}]+|sinosi\s+[^\}]+|else\s*if\s+[^\}]+|elif\s+[^\}]+|sino|else|/si|/if|fin\s*si|finsi|endif)\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool HasConditionals(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return false;
        }

        return TagFinderRegex.IsMatch(content);
    }

    /// <summary>
    /// Procesa y evalúa todos los bloques condicionales presentes en el contenido,
    /// admitiendo anidación de condicionales.
    /// </summary>
    public static string ProcessConditionals(string? content, ConditionContext context)
    {
        if (string.IsNullOrEmpty(content) || !HasConditionals(content))
        {
            return content ?? string.Empty;
        }

        var sb = new StringBuilder();
        int currentIndex = 0;

        while (currentIndex < content.Length)
        {
            // Buscar la próxima etiqueta de apertura {si ...} o {if ...}
            var matches = TagFinderRegex.Matches(content, currentIndex);
            Match? openMatch = null;

            foreach (Match m in matches)
            {
                if (IfOpenRegex.IsMatch(m.Value))
                {
                    openMatch = m;
                    break;
                }
            }

            if (openMatch == null)
            {
                // No hay más aperturas, añadir el resto del contenido
                sb.Append(content[currentIndex..]);
                break;
            }

            // Añadir el contenido previo a la apertura
            sb.Append(content[currentIndex..openMatch.Index]);

            // Encontrar el bloque completo balanceado
            int blockStartIndex = openMatch.Index;
            int blockEndIndex = -1;
            int depth = 0;

            var branchConditions = new List<string>();
            var branchContentIndices = new List<(int start, int end)>();

            string initialCondition = IfOpenRegex.Match(openMatch.Value).Groups[1].Value.Trim();
            branchConditions.Add(initialCondition);

            int lastBranchContentStart = openMatch.Index + openMatch.Length;

            foreach (Match m in TagFinderRegex.Matches(content, openMatch.Index))
            {
                var tag = m.Value;

                if (IfOpenRegex.IsMatch(tag))
                {
                    depth++;
                }
                else if (EndIfRegex.IsMatch(tag))
                {
                    depth--;
                    if (depth == 0)
                    {
                        branchContentIndices.Add((lastBranchContentStart, m.Index));
                        blockEndIndex = m.Index + m.Length;
                        break;
                    }
                }
                else if (depth == 1)
                {
                    if (ElifRegex.IsMatch(tag))
                    {
                        branchContentIndices.Add((lastBranchContentStart, m.Index));
                        string elifCondition = ElifRegex.Match(tag).Groups[1].Value.Trim();
                        branchConditions.Add(elifCondition);
                        lastBranchContentStart = m.Index + m.Length;
                    }
                    else if (ElseRegex.IsMatch(tag))
                    {
                        branchContentIndices.Add((lastBranchContentStart, m.Index));
                        branchConditions.Add("true"); // La rama else siempre es candidata si las anteriores fallan
                        lastBranchContentStart = m.Index + m.Length;
                    }
                }
            }

            if (blockEndIndex == -1)
            {
                // Bloque no cerrado correctamente; conservar sin procesar para no perder texto
                sb.Append(content[openMatch.Index..]);
                break;
            }

            // Evaluar las ramas del bloque
            string chosenText = string.Empty;
            for (int i = 0; i < branchConditions.Count; i++)
            {
                string condition = branchConditions[i];
                if (ExpressionEvaluator.Evaluate(condition, context))
                {
                    var (start, end) = branchContentIndices[i];
                    string rawBranchContent = content[start..end];
                    chosenText = TrimLeadingAndTrailingNewlines(rawBranchContent);
                    break;
                }
            }

            // Procesar recursivamente el contenido de la rama elegida (para resolver ifs anidados)
            string evaluatedBranch = ProcessConditionals(chosenText, context);
            sb.Append(evaluatedBranch);

            currentIndex = blockEndIndex;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Limpia un salto de línea inicial o final si la etiqueta condicional estaba aislada en su propia línea.
    /// </summary>
    private static string TrimLeadingAndTrailingNewlines(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        string result = text;
        if (result.StartsWith("\r\n"))
        {
            result = result[2..];
        }
        else if (result.StartsWith("\n"))
        {
            result = result[1..];
        }

        if (result.EndsWith("\r\n"))
        {
            result = result[..^2];
        }
        else if (result.EndsWith("\n"))
        {
            result = result[..^1];
        }

        return result;
    }
}
