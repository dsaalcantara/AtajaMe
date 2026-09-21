using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Enums;

namespace AtajaMe.Application.Csv.DTOs;

/// <summary>
/// Vista previa del estado de importación de una fila específica de CSV.
/// </summary>
public sealed record CsvImportItemPreview(
    int RowIndex,
    CsvSnippetRow Row,
    bool HasConflict,
    Snippet? ExistingSnippet = null,
    string? ValidationErrorMessage = null)
{
    public bool IsValid => string.IsNullOrEmpty(ValidationErrorMessage);
}

/// <summary>
/// Resumen global de la inspección previa de importación CSV.
/// </summary>
public sealed record CsvImportPreview(
    int TotalRows,
    int ValidRows,
    int ConflictCount,
    int InvalidRows,
    IReadOnlyList<CsvImportItemPreview> Items);
