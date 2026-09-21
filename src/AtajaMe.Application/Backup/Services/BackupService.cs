using System.IO;
using AtajaMe.Application.Backup.DTOs;

namespace AtajaMe.Application.Backup.Services;

/// <summary>
/// Orquestador de casos de uso para generación de copias de seguridad, retención y restauración (RF-28 a RF-32).
/// </summary>
public sealed class BackupService : IBackupService
{
    private readonly ISqliteBackupProvider _backupProvider;
    private readonly IBackupStorage _storage;
    private readonly BackupPolicy _policy;

    public BackupService(
        ISqliteBackupProvider backupProvider,
        IBackupStorage storage,
        BackupPolicy? policy = null)
    {
        _backupProvider = backupProvider ?? throw new ArgumentNullException(nameof(backupProvider));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _policy = policy ?? new BackupPolicy();
    }

    public async Task<BackupFileInfoDto> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        var timestamp = DateTime.UtcNow;
        var filePath = _storage.GenerateBackupFilePath(timestamp);

        await _backupProvider.BackupToAsync(filePath, cancellationToken);

        // Aplicar la política de retención configurada (default 10)
        await ApplyRetentionPolicyAsync(_policy.MaxCopiesToKeep, cancellationToken);

        var fileInfo = new FileInfo(filePath);
        return new BackupFileInfoDto(
            FilePath: filePath,
            FileName: fileInfo.Name,
            FileSizeBytes: fileInfo.Exists ? fileInfo.Length : 0,
            CreatedAtUtc: timestamp);
    }

    public Task<IReadOnlyList<BackupFileInfoDto>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        var list = _storage.ListBackups();
        return Task.FromResult(list);
    }

    public async Task<BackupPreviewDto> GetBackupPreviewAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupFilePath))
        {
            throw new ArgumentException("La ruta del archivo de respaldo no puede estar vacía.", nameof(backupFilePath));
        }

        if (!File.Exists(backupFilePath))
        {
            throw new FileNotFoundException("El archivo de copia de seguridad no existe.", backupFilePath);
        }

        return await _backupProvider.InspectBackupAsync(backupFilePath, cancellationToken);
    }

    public async Task RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupFilePath))
        {
            throw new ArgumentException("La ruta del archivo de respaldo no puede estar vacía.", nameof(backupFilePath));
        }

        if (!File.Exists(backupFilePath))
        {
            throw new FileNotFoundException("El archivo de copia de seguridad no existe.", backupFilePath);
        }

        await _backupProvider.RestoreFromAsync(backupFilePath, cancellationToken);
    }

    public Task ApplyRetentionPolicyAsync(int maxCopiesToKeep = 10, CancellationToken cancellationToken = default)
    {
        if (maxCopiesToKeep <= 0)
        {
            return Task.CompletedTask;
        }

        var backups = _storage.ListBackups();
        if (backups.Count > maxCopiesToKeep)
        {
            var surplus = backups.Skip(maxCopiesToKeep);
            foreach (var oldBackup in surplus)
            {
                try
                {
                    _storage.DeleteBackup(oldBackup.FilePath);
                }
                catch
                {
                    // Evitar que un fallo al eliminar un archivo individual detenga el proceso
                }
            }
        }

        return Task.CompletedTask;
    }
}
