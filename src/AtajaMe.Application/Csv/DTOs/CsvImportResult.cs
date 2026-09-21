namespace AtajaMe.Application.Csv.DTOs;

/// <summary>
/// Resultado final de la ejecución de una importación CSV.
/// </summary>
public sealed record CsvImportResult(
    int TotalProcessed,
    int ImportedCount,
    int OverwrittenCount,
    int DuplicatedCount,
    int SkippedCount,
    IReadOnlyList<string> Errors)
{
    public bool HasErrors => Errors.Count > 0;
}
