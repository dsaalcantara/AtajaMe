using System.Text;
using AtajaMe.Application.Csv.DTOs;

namespace AtajaMe.Application.Csv.Parser;

/// <summary>
/// Parser robusto de archivos CSV conforme a RFC 4180.
/// Soporta campos multilínea entrecomillados, comillas escapadas (""),
/// flexibilidad en el orden de columnas y omisión transparente de BOM.
/// </summary>
public static class CsvReaderHelper
{
    public static IReadOnlyList<CsvSnippetRow> Read(TextReader reader)
    {
        var rawRows = ParseRawRows(reader);
        if (rawRows.Count == 0)
        {
            return Array.Empty<CsvSnippetRow>();
        }

        var headerTokens = rawRows[0];
        var columnIndexMap = BuildColumnIndexMap(headerTokens);

        var result = new List<CsvSnippetRow>(rawRows.Count - 1);
        for (int i = 1; i < rawRows.Count; i++)
        {
            var tokens = rawRows[i];
            if (tokens.Count == 0 || (tokens.Count == 1 && string.IsNullOrWhiteSpace(tokens[0])))
            {
                continue; // Fila vacía
            }

            var row = MapRow(tokens, columnIndexMap);
            result.Add(row);
        }

        return result;
    }

    public static IReadOnlyList<IReadOnlyList<string>> ParseRawRows(TextReader reader)
    {
        var rows = new List<IReadOnlyList<string>>();
        var currentField = new StringBuilder();
        var currentFields = new List<string>();

        bool inQuotes = false;
        int prevChar = -1;
        int c;

        while ((c = reader.Read()) != -1)
        {
            char ch = (char)c;

            // Limpieza de Byte Order Mark (BOM) en el primer caracter si existe
            if (ch == '\uFEFF')
            {
                continue;
            }

            if (inQuotes)
            {
                if (ch == '"')
                {
                    int next = reader.Peek();
                    if (next == '"')
                    {
                        // Comilla escapada ""
                        currentField.Append('"');
                        reader.Read(); // Consume la segunda comilla
                    }
                    else
                    {
                        // Fin de campo entrecomillado
                        inQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(ch);
                }
            }
            else
            {
                if (ch == '"')
                {
                    inQuotes = true;
                }
                else if (ch == ',')
                {
                    currentFields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else if (ch == '\r')
                {
                    // Si viene seguido de \n, el siguiente caracter lo completará
                    currentFields.Add(currentField.ToString());
                    currentField.Clear();
                    rows.Add(currentFields.ToArray());
                    currentFields = new List<string>();

                    if (reader.Peek() == '\n')
                    {
                        reader.Read();
                    }
                }
                else if (ch == '\n')
                {
                    currentFields.Add(currentField.ToString());
                    currentField.Clear();
                    rows.Add(currentFields.ToArray());
                    currentFields = new List<string>();
                }
                else
                {
                    currentField.Append(ch);
                }
            }

            prevChar = c;
        }

        // Añadir el último campo y fila si no concluyó con salto de línea
        if (currentField.Length > 0 || currentFields.Count > 0 || inQuotes)
        {
            currentFields.Add(currentField.ToString());
            rows.Add(currentFields.ToArray());
        }

        return rows;
    }

    private static Dictionary<string, int> BuildColumnIndexMap(IReadOnlyList<string> headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Count; i++)
        {
            var header = headers[i].Trim();
            if (!string.IsNullOrEmpty(header) && !map.ContainsKey(header))
            {
                map[header] = i;
            }
        }
        return map;
    }

    private static CsvSnippetRow MapRow(IReadOnlyList<string> tokens, Dictionary<string, int> map)
    {
        string GetValue(string columnName, string defaultValue = "")
        {
            if (map.TryGetValue(columnName, out int index) && index < tokens.Count)
            {
                return tokens[index];
            }
            return defaultValue;
        }

        string GetValueAny(string[] columnNames, string defaultValue = "")
        {
            foreach (var col in columnNames)
            {
                if (map.TryGetValue(col, out int index) && index < tokens.Count && !string.IsNullOrEmpty(tokens[index]))
                {
                    return tokens[index];
                }
            }
            return defaultValue;
        }

        bool GetBool(string columnName, bool defaultValue = false)
        {
            var val = GetValue(columnName, string.Empty).Trim();
            if (string.Equals(val, "true", StringComparison.OrdinalIgnoreCase) || val == "1")
            {
                return true;
            }
            if (string.Equals(val, "false", StringComparison.OrdinalIgnoreCase) || val == "0")
            {
                return false;
            }
            return defaultValue;
        }

        int GetInt(string columnName, int defaultValue = 0)
        {
            var val = GetValue(columnName, string.Empty).Trim();
            return int.TryParse(val, out int parsed) ? parsed : defaultValue;
        }

        string code = GetValueAny(new[] { "Code", "Shortcut", "Autotext", "Trigger", "Abbreviation", "Atajo", "Código" });
        string name = GetValueAny(new[] { "Name", "Description", "Title", "Nombre", "Descripción" });
        string content = GetValueAny(new[] { "Content", "Phrase", "Text", "Body", "Snippet", "Contenido", "Texto" });
        string folderPath = GetValueAny(new[] { "FolderPath", "Folder", "Category", "Carpeta", "Categoría" });
        bool caseSensitive = GetBool("CaseSensitive", false);
        bool isActive = GetBool("IsActive", true);
        bool isFavorite = GetBool("IsFavorite", false);
        int sortOrder = GetInt("SortOrder", 0);

        return new CsvSnippetRow(
            Code: code,
            Name: string.IsNullOrWhiteSpace(name) ? code : name,
            Content: content,
            FolderPath: folderPath,
            CaseSensitive: caseSensitive,
            IsActive: isActive,
            IsFavorite: isFavorite,
            SortOrder: sortOrder);
    }
}
