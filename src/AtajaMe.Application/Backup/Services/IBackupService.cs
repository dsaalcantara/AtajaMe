using AtajaMe.Application.Backup.DTOs;

namespace AtajaMe.Application.Backup.Services;

/// <summary>
/// Contrato del servicio de copias de seguridad, retención y restauración (RF-28 a RF-32).
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Genera una copia de seguridad consistente inmediata y aplica la política de retención automática (RF-28, RF-31).
    /// </summary>
    Task<BackupFileInfoDto> CreateBackupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los archivos de respaldo disponibles ordenados del más reciente al más antiguo.
    /// </summary>
    Task<IReadOnlyList<BackupFileInfoDto>> ListBackupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene la vista previa y metadatos de un archivo de respaldo específico (RF-32).
    /// </summary>
    Task<BackupPreviewDto> GetBackupPreviewAsync(string backupFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restaura la base de datos viva a partir del archivo de copia de seguridad seleccionado (RF-32).
    /// </summary>
    Task RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aplica la política de retención eliminando las copias excedentes (por defecto conservando las 10 más recientes) (RF-31).
    /// </summary>
    Task ApplyRetentionPolicyAsync(int maxCopiesToKeep = 10, CancellationToken cancellationToken = default);
}
