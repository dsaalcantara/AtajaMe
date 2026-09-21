namespace AtajaMe.Application.Csv.DTOs;

/// <summary>
/// Mapeo plano de una fila en el archivo CSV de atajos.
/// </summary>
public sealed record CsvSnippetRow(
    string Code,
    string Name,
    string Content,
    string FolderPath = "",
    bool CaseSensitive = false,
    bool IsActive = true,
    bool IsFavorite = false,
    int SortOrder = 0);
