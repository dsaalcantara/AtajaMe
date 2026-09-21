using System.Text;
using AtajaMe.Application.Csv.DTOs;

namespace AtajaMe.Application.Csv.Parser;

/// <summary>
/// Serializador de filas de atajos a formato CSV conforme a RFC 4180.
/// </summary>
public static class CsvWriterHelper
{
    public const string Header = "Code,Name,Content,FolderPath,CaseSensitive,IsActive,IsFavorite,SortOrder";

    public static string EscapeField(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        bool mustQuote = value.Contains(',') ||
                         value.Contains('"') ||
                         value.Contains('\r') ||
                         value.Contains('\n');

        if (!mustQuote)
        {
            return value;
        }

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    public static string FormatRow(CsvSnippetRow row)
    {
        var sb = new StringBuilder();
        sb.Append(EscapeField(row.Code)).Append(',');
        sb.Append(EscapeField(row.Name)).Append(',');
        sb.Append(EscapeField(row.Content)).Append(',');
        sb.Append(EscapeField(row.FolderPath)).Append(',');
        sb.Append(row.CaseSensitive ? "true" : "false").Append(',');
        sb.Append(row.IsActive ? "true" : "false").Append(',');
        sb.Append(row.IsFavorite ? "true" : "false").Append(',');
        sb.Append(row.SortOrder);
        return sb.ToString();
    }

    public static void Write(TextWriter writer, IEnumerable<CsvSnippetRow> rows)
    {
        writer.WriteLine(Header);
        foreach (var row in rows)
        {
            writer.WriteLine(FormatRow(row));
        }
    }

    public static string ExportToString(IEnumerable<CsvSnippetRow> rows)
    {
        using var stringWriter = new StringWriter();
        Write(stringWriter, rows);
        return stringWriter.ToString();
    }
}
