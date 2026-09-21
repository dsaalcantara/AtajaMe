using AtajaMe.Application.Csv.DTOs;
using AtajaMe.Domain.Enums;

namespace AtajaMe.Application.Csv.Services;

/// <summary>
/// Contrato de servicio para la importación y exportación de atajos en formato CSV (RF-25, RF-26, RF-27).
/// </summary>
public interface ICsvImportExportService
{
    /// <summary>
    /// Exporta toda la biblioteca activa a formato CSV conservando la estructura de carpetas (RF-27).
    /// </summary>
    Task ExportAllAsync(TextWriter writer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exporta los atajos de una carpeta concreta y sus subcarpetas descendientes (RF-27).
    /// </summary>
    Task ExportFolderAsync(Guid folderId, TextWriter writer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exporta un atajo individual por su identificador (RF-27).
    /// </summary>
    Task ExportSnippetAsync(Guid snippetId, TextWriter writer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inspecciona un archivo CSV contra la base local identificando atajos válidos y posibles colisiones de código (RF-26).
    /// </summary>
    Task<CsvImportPreview> AnalyzeImportAsync(TextReader reader, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta la importación del CSV aplicando resoluciones caso por caso y reconstruyendo la jerarquía de carpetas (RF-26).
    /// </summary>
    Task<CsvImportResult> ExecuteImportAsync(
        TextReader reader,
        IReadOnlyDictionary<string, CsvConflictResolution>? resolutionOverrides = null,
        CsvConflictResolution defaultResolution = CsvConflictResolution.Skip,
        CancellationToken cancellationToken = default);
}
