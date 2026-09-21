using System.IO;
using AtajaMe.Application.Backup.DTOs;
using AtajaMe.Application.Backup.Services;
using AtajaMe.Domain.Entities;
using AtajaMe.Infrastructure.Backup;
using AtajaMe.Infrastructure.Persistence;
using AtajaMe.Infrastructure.Repositories;
using AtajaMe.Tests.Infrastructure;

namespace AtajaMe.Tests.Backup;

public class BackupServiceTests : SqliteTestBase
{
    private readonly string _tempBackupDir;

    public BackupServiceTests()
    {
        _tempBackupDir = Path.Combine(Path.GetTempPath(), $"AtajaMe_BackupTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempBackupDir);
    }

    public override void Dispose()
    {
        base.Dispose();
        try
        {
            if (Directory.Exists(_tempBackupDir))
            {
                Directory.Delete(_tempBackupDir, true);
            }
        }
        catch
        {
            // Limpieza sin bloquear
        }
    }

    [Fact]
    public async Task CreateBackupAsync_CreatesValidFileWithCorrectNaming()
    {
        var snippetRepo = new SnippetRepository(ConnectionFactory);
        await snippetRepo.AddAsync(Snippet.Create("!b1", "Test Backup", "Contenido"));

        var backupProvider = new SqliteBackupProvider(ConnectionFactory);
        var storage = new DiskBackupStorage(_tempBackupDir);
        var service = new BackupService(backupProvider, storage, new BackupPolicy(10));

        var backupInfo = await service.CreateBackupAsync();

        Assert.NotNull(backupInfo);
        Assert.True(File.Exists(backupInfo.FilePath));
        Assert.StartsWith("AtajaMe_Backup_", backupInfo.FileName);
        Assert.EndsWith(".db", backupInfo.FileName);
        Assert.True(backupInfo.FileSizeBytes > 0);
    }

    [Fact]
    public async Task RetentionPolicy_RetainsOnlyConfiguredMaxCopies()
    {
        var backupProvider = new SqliteBackupProvider(ConnectionFactory);
        var storage = new DiskBackupStorage(_tempBackupDir);
        var service = new BackupService(backupProvider, storage, new BackupPolicy(10));

        // Simular 15 archivos de backup con fechas consecutivas
        var baseDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 15; i++)
        {
            var date = baseDate.AddMinutes(i);
            var path = storage.GenerateBackupFilePath(date);
            await File.WriteAllTextAsync(path, $"fake_db_content_{i}");
        }

        var initialList = storage.ListBackups();
        Assert.Equal(15, initialList.Count);

        // Aplicar retención de 10 copias
        await service.ApplyRetentionPolicyAsync(10);

        var finalList = storage.ListBackups();
        Assert.Equal(10, finalList.Count);

        // Las 10 copias conservadas deben ser las más recientes (i = 5 a 14)
        var oldestRemainingDate = baseDate.AddMinutes(5);
        Assert.True(finalList.Min(b => b.CreatedAtUtc) >= oldestRemainingDate);
    }
}
