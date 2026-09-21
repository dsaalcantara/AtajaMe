using System.Globalization;
using System.IO;
using AtajaMe.Application.Backup.DTOs;
using AtajaMe.Application.Backup.Services;

namespace AtajaMe.Infrastructure.Backup;

/// <summary>
/// Implementación de IBackupStorage para administración de respaldos en el sistema de archivos local (RF-30, RF-31).
/// </summary>
public sealed class DiskBackupStorage : IBackupStorage
{
    private const string BackupFilePrefix = "AtajaMe_Backup_";
    private const string BackupFileExtension = ".db";
    private const string TimestampFormat = "yyyyMMdd_HHmmss";

    private readonly string _backupDirectory;

    public DiskBackupStorage(string? backupDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(backupDirectory))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _backupDirectory = Path.Combine(appData, "AtajaMe", "Backups");
        }
        else
        {
            _backupDirectory = backupDirectory;
        }

        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
        }
    }

    public string GetBackupDirectory() => _backupDirectory;

    public string GenerateBackupFilePath(DateTime timestampUtc)
    {
        var fileName = $"{BackupFilePrefix}{timestampUtc.ToString(TimestampFormat, CultureInfo.InvariantCulture)}{BackupFileExtension}";
        return Path.Combine(_backupDirectory, fileName);
    }

    public IReadOnlyList<BackupFileInfoDto> ListBackups()
    {
        if (!Directory.Exists(_backupDirectory))
        {
            return Array.Empty<BackupFileInfoDto>();
        }

        var dirInfo = new DirectoryInfo(_backupDirectory);
        var files = dirInfo.GetFiles($"{BackupFilePrefix}*{BackupFileExtension}");

        var result = new List<BackupFileInfoDto>(files.Length);

        foreach (var file in files)
        {
            var timestamp = ExtractTimestampFromFileName(file.Name) ?? file.CreationTimeUtc;
            result.Add(new BackupFileInfoDto(
                FilePath: file.FullName,
                FileName: file.Name,
                FileSizeBytes: file.Length,
                CreatedAtUtc: timestamp));
        }

        // Ordenar del más reciente al más antiguo
        return result.OrderByDescending(b => b.CreatedAtUtc).ThenByDescending(b => b.FileName).ToList();
    }

    public void DeleteBackup(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private static DateTime? ExtractTimestampFromFileName(string fileName)
    {
        // Formato: AtajaMe_Backup_YYYYMMDD_HHMMSS.db
        if (!fileName.StartsWith(BackupFilePrefix, StringComparison.OrdinalIgnoreCase) ||
            !fileName.EndsWith(BackupFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        int start = BackupFilePrefix.Length;
        int length = fileName.Length - start - BackupFileExtension.Length;

        if (length != TimestampFormat.Length)
        {
            return null;
        }

        var rawTimestamp = fileName.Substring(start, length);
        if (DateTime.TryParseExact(rawTimestamp, TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
