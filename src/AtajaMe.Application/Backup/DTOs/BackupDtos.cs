namespace AtajaMe.Application.Backup.DTOs;

/// <summary>
/// Resumen de contenido de un archivo de copia de seguridad para inspección previa a su restauración (RF-32).
/// </summary>
public sealed record BackupPreviewDto(
    string FilePath,
    string FileName,
    long FileSizeBytes,
    DateTime CreatedAtUtc,
    int TotalSnippets,
    int TotalFolders,
    bool IsValid);

/// <summary>
/// Información de un archivo de respaldo almacenado en disco.
/// </summary>
public sealed record BackupFileInfoDto(
    string FilePath,
    string FileName,
    long FileSizeBytes,
    DateTime CreatedAtUtc);

/// <summary>
/// Política de retención y configuración de copias de seguridad (RF-29, RF-30, RF-31).
/// </summary>
public sealed record BackupPolicy(
    int MaxCopiesToKeep = 10,
    string? CustomBackupDirectory = null);
