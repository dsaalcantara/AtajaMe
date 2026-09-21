using System.IO;
using AtajaMe.Application.Sync.Services;
using AtajaMe.Domain.Entities;
using AtajaMe.Infrastructure.Repositories;
using AtajaMe.Infrastructure.Sync;
using AtajaMe.Tests.Infrastructure;

namespace AtajaMe.Tests.Sync;

public class SyncResilienceTests : SqliteTestBase
{
    [Fact]
    public async Task ProcessRemoteManifestAsync_WhenDirectoryDoesNotExist_ReturnsUnavailableWithoutThrowing()
    {
        var snippetRepo = new SnippetRepository(ConnectionFactory);
        var folderRepo = new FolderRepository(ConnectionFactory);
        var metadataRepo = new SyncMetadataRepository(ConnectionFactory);

        // Directorio ficticio inexistente (ej. disco de red desconectado)
        string nonExistentPath = Path.Combine(Path.GetTempPath(), $"NonExistentDrive_{Guid.NewGuid():N}", "AtajaMeSync");
        var syncStorage = new DiskSyncStorage(nonExistentPath);

        var engine = new SyncEngine(snippetRepo, folderRepo, metadataRepo, syncStorage);

        // No debe arrojar excepción no controlada (RF-23)
        var result = await engine.ProcessRemoteManifestAsync();

        Assert.False(result.Success);
        Assert.False(result.HasConflicts);
        Assert.Contains("no está accesible", result.Message!);

        // La base de datos local y los atajos deben seguir funcionando con total normalidad
        var snippet = Snippet.Create("!local_ok", "Local", "Funcionando local");
        await snippetRepo.AddAsync(snippet);

        var retrieved = await snippetRepo.GetByCodeAsync("!local_ok");
        Assert.NotNull(retrieved);
        Assert.Equal("Funcionando local", retrieved.Content);
    }
}
