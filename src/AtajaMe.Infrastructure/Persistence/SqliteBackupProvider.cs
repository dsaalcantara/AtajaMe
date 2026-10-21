using System.IO;
using AtajaMe.Application.Backup.DTOs;
using AtajaMe.Application.Backup.Services;
using Microsoft.Data.Sqlite;

namespace AtajaMe.Infrastructure.Persistence;

/// <summary>
/// Implementación de ISqliteBackupProvider mediante la API de bajo nivel SqliteConnection.BackupDatabase (RF-28, RF-32).
/// Garantiza copias en caliente consistentes sin bloqueos de archivo.
/// </summary>
public sealed class SqliteBackupProvider : ISqliteBackupProvider
{
    private readonly ISqliteConnectionFactory _connectionFactory;
    private readonly Action? _onDatabaseRestored;

    public SqliteBackupProvider(ISqliteConnectionFactory connectionFactory, Action? onDatabaseRestored = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _onDatabaseRestored = onDatabaseRestored;
    }

    public async Task BackupToAsync(string destinationFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationFilePath))
        {
            throw new ArgumentException("La ruta de destino no puede estar vacía.", nameof(destinationFilePath));
        }

        var directory = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var sourceConnection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var destConnection = new SqliteConnection($"Data Source={destinationFilePath}");
        await destConnection.OpenAsync(cancellationToken);

        sourceConnection.BackupDatabase(destConnection);
    }

    public async Task RestoreFromAsync(string sourceBackupFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceBackupFilePath) || !File.Exists(sourceBackupFilePath))
        {
            throw new FileNotFoundException("El archivo de copia de seguridad no existe.", sourceBackupFilePath);
        }

        await using var sourceBackupConnection = new SqliteConnection($"Data Source={sourceBackupFilePath};Mode=ReadOnly");
        await sourceBackupConnection.OpenAsync(cancellationToken);

        await using var liveConnection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        sourceBackupConnection.BackupDatabase(liveConnection);
        _onDatabaseRestored?.Invoke();
    }

    public async Task<BackupPreviewDto> InspectBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
        {
            throw new FileNotFoundException("El archivo de copia de seguridad no existe.", backupFilePath);
        }

        var fileInfo = new FileInfo(backupFilePath);
        int totalSnippets = 0;
        int totalFolders = 0;
        bool isValid = false;

        try
        {
            await using var connection = new SqliteConnection($"Data Source={backupFilePath};Mode=ReadOnly");
            await connection.OpenAsync(cancellationToken);

            // Contar atajos
            await using (var cmdSnippets = connection.CreateCommand())
            {
                cmdSnippets.CommandText = "SELECT COUNT(*) FROM Snippets WHERE IsInTrash = 0;";
                var result = await cmdSnippets.ExecuteScalarAsync(cancellationToken);
                totalSnippets = Convert.ToInt32(result);
            }

            // Contar carpetas
            await using (var cmdFolders = connection.CreateCommand())
            {
                cmdFolders.CommandText = "SELECT COUNT(*) FROM Folders;";
                var result = await cmdFolders.ExecuteScalarAsync(cancellationToken);
                totalFolders = Convert.ToInt32(result);
            }

            isValid = true;
        }
        catch
        {
            isValid = false;
        }

        return new BackupPreviewDto(
            FilePath: backupFilePath,
            FileName: fileInfo.Name,
            FileSizeBytes: fileInfo.Length,
            CreatedAtUtc: fileInfo.CreationTimeUtc,
            TotalSnippets: totalSnippets,
            TotalFolders: totalFolders,
            IsValid: isValid);
    }
}
