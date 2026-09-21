using System.IO;
using AtajaMe.Application.Backup.DTOs;
using AtajaMe.Application.Backup.Services;
using AtajaMe.Domain.Entities;
using AtajaMe.Infrastructure.Backup;
using AtajaMe.Infrastructure.Persistence;
using AtajaMe.Infrastructure.Repositories;
using AtajaMe.Tests.Infrastructure;

namespace AtajaMe.Tests.Backup;

public class BackupRestoreTests : SqliteTestBase
{
    private readonly string _tempBackupDir;

    public BackupRestoreTests()
    {
        _tempBackupDir = Path.Combine(Path.GetTempPath(), $"AtajaMe_RestoreTests_{Guid.NewGuid():N}");
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
        }
    }

    [Fact]
    public async Task GetBackupPreviewAsync_ReturnsAccurateCounts()
    {
        var snippetRepo = new SnippetRepository(ConnectionFactory);
        var folderRepo = new FolderRepository(ConnectionFactory);

        var folder = Folder.Create("Carpeta Preview");
        await folderRepo.AddAsync(folder);

        await snippetRepo.AddAsync(Snippet.Create("!s1", "S1", "Contenido 1", folder.Id));
        await snippetRepo.AddAsync(Snippet.Create("!s2", "S2", "Contenido 2", folder.Id));

        var backupProvider = new SqliteBackupProvider(ConnectionFactory);
        var storage = new DiskBackupStorage(_tempBackupDir);
        var service = new BackupService(backupProvider, storage);

        var backupInfo = await service.CreateBackupAsync();

        var preview = await service.GetBackupPreviewAsync(backupInfo.FilePath);

        Assert.True(preview.IsValid);
        Assert.Equal(2, preview.TotalSnippets);
        Assert.Equal(1, preview.TotalFolders);
        Assert.True(preview.FileSizeBytes > 0);
    }

    [Fact]
    public async Task RestoreBackupAsync_RestoresDatabaseStateConsistently()
    {
        var snippetRepo = new SnippetRepository(ConnectionFactory);
        var folderRepo = new FolderRepository(ConnectionFactory);

        // Estado inicial
        var f1 = Folder.Create("Carpeta Original");
        await folderRepo.AddAsync(f1);

        var s1 = Snippet.Create("!original", "Original", "Contenido Original", f1.Id);
        await snippetRepo.AddAsync(s1);

        var backupProvider = new SqliteBackupProvider(ConnectionFactory);
        var storage = new DiskBackupStorage(_tempBackupDir);
        var service = new BackupService(backupProvider, storage);

        // Generar copia de seguridad del estado inicial
        var backupInfo = await service.CreateBackupAsync();

        // Mutar la base viva
        var s2 = Snippet.Create("!nuevo", "Nuevo Post Backup", "Contenido Nuevo");
        await snippetRepo.AddAsync(s2);
        await snippetRepo.DeletePermanentAsync(s1.Id);

        var mutatedSnippets = await snippetRepo.GetAllActiveAsync();
        Assert.Single(mutatedSnippets);
        Assert.Equal("!nuevo", mutatedSnippets[0].Code);

        // Restaurar la copia de seguridad
        await service.RestoreBackupAsync(backupInfo.FilePath);

        // Verificar que el estado volvió al inicial
        var restoredSnippets = await snippetRepo.GetAllActiveAsync();
        Assert.Single(restoredSnippets);
        Assert.Equal("!original", restoredSnippets[0].Code);
        Assert.Equal("Contenido Original", restoredSnippets[0].Content);

        var restoredFolders = await folderRepo.GetAllAsync();
        Assert.Single(restoredFolders);
        Assert.Equal("Carpeta Original", restoredFolders[0].Name);
    }
}
