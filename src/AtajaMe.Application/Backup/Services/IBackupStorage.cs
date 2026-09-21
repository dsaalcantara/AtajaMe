using AtajaMe.Application.Backup.DTOs;

namespace AtajaMe.Application.Backup.Services;

/// <summary>
/// Contrato para la gestión de archivos y rutas de copias de seguridad en disco.
/// </summary>
public interface IBackupStorage
{
    /// <summary>
    /// Obtiene el directorio configurado para almacenar las copias de seguridad.
    /// </summary>
    string GetBackupDirectory();

    /// <summary>
    /// Genera la ruta completa normalizada para una nueva copia con nomenclatura AtajaMe_Backup_YYYYMMDD_HHMMSS.db.
    /// </summary>
    string GenerateBackupFilePath(DateTime timestampUtc);

    /// <summary>
    /// Lista todos los archivos de respaldo existentes ordenados por fecha descendente.
    /// </summary>
    IReadOnlyList<BackupFileInfoDto> ListBackups();

    /// <summary>
    /// Elimina un archivo de respaldo específico.
    /// </summary>
    void DeleteBackup(string filePath);
}
