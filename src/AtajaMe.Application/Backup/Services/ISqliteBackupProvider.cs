using AtajaMe.Application.Backup.DTOs;

namespace AtajaMe.Application.Backup.Services;

/// <summary>
/// Contrato de infraestructura para operaciones de backup y restauración a nivel de SQLite (RF-28, RF-32).
/// Desacopla la capa de aplicación de las clases concretas de Microsoft.Data.Sqlite.
/// </summary>
public interface ISqliteBackupProvider
{
    /// <summary>
    /// Genera un volcado consistente de la base de datos viva hacia el archivo destino indicado.
    /// </summary>
    Task BackupToAsync(string destinationFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restaura la base de datos viva aplicando el contenido íntegro del archivo de respaldo especificado.
    /// </summary>
    Task RestoreFromAsync(string sourceBackupFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inspecciona el contenido de un archivo de copia de seguridad sin modificar la base viva.
    /// </summary>
    Task<BackupPreviewDto> InspectBackupAsync(string backupFilePath, CancellationToken cancellationToken = default);
}
