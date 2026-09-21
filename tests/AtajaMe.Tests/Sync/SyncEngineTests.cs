using System.IO;
using AtajaMe.Application.Sync.DTOs;
using AtajaMe.Application.Sync.Services;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Enums;
using AtajaMe.Infrastructure.Repositories;
using AtajaMe.Infrastructure.Sync;
using AtajaMe.Tests.Infrastructure;

namespace AtajaMe.Tests.Sync;

public class SyncEngineTests : SqliteTestBase
{
    private readonly string _tempSyncDir;

    public SyncEngineTests()
    {
        _tempSyncDir = Path.Combine(Path.GetTempPath(), $"AtajaMe_SyncTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempSyncDir);
    }

    public override void Dispose()
    {
        base.Dispose();
        try
        {
            if (Directory.Exists(_tempSyncDir))
            {
                Directory.Delete(_tempSyncDir, true);
            }
        }
        catch
        {
        }
    }

    [Fact]
    public async Task ExportManifestAsync_GeneratesConsistentJsonWithHash()
    {
        var snippetRepo = new SnippetRepository(ConnectionFactory);
        var folderRepo = new FolderRepository(ConnectionFactory);
        var metadataRepo = new SyncMetadataRepository(ConnectionFactory);
        var syncStorage = new DiskSyncStorage(_tempSyncDir);

        var folder = Folder.Create("Trabajo");
        await folderRepo.AddAsync(folder);
        await snippetRepo.AddAsync(Snippet.Create("!e1", "Email", "correo@test.com", folder.Id));

        var engine = new SyncEngine(snippetRepo, folderRepo, metadataRepo, syncStorage);

        var manifest = await engine.ExportManifestAsync();

        Assert.NotNull(manifest);
        Assert.Single(manifest.Folders);
        Assert.Single(manifest.Snippets);
        Assert.False(string.IsNullOrEmpty(manifest.ManifestHash));

        var manifestFile = Path.Combine(_tempSyncDir, "sync_manifest.json");
        Assert.True(File.Exists(manifestFile));

        var metadata = await metadataRepo.GetAsync();
        Assert.NotNull(metadata);
        Assert.Equal(manifest.ManifestHash, metadata.LastSyncHash);
    }

    [Fact]
    public async Task ProcessRemoteManifestAsync_ImportsRemoteSnippetsAutomaticallyWhenNoConflict()
    {
        var snippetRepo = new SnippetRepository(ConnectionFactory);
        var folderRepo = new FolderRepository(ConnectionFactory);
        var metadataRepo = new SyncMetadataRepository(ConnectionFactory);
        var syncStorage = new DiskSyncStorage(_tempSyncDir);

        var engine = new SyncEngine(snippetRepo, folderRepo, metadataRepo, syncStorage);

        // Crear manifiesto emitido por otra máquina
        var remoteFolder = new SyncFolderDto(Guid.NewGuid(), "RemotoFolder", null, 0, DateTime.UtcNow, DateTime.UtcNow);
        var remoteSnippet = new SyncSnippetDto(
            Guid.NewGuid(), "!remoto", "Remoto", "Texto desde otra máquina", remoteFolder.Id,
            false, true, false, false, null, 0, 0, null, DateTime.UtcNow, DateTime.UtcNow);

        var remoteManifest = new SyncManifestDto(
            1, "OTHER_MACHINE_ID", DateTime.UtcNow,
            new[] { remoteFolder }, new[] { remoteSnippet }, Array.Empty<Guid>(), "REMOTE_HASH_123");

        await syncStorage.WriteManifestAsync(remoteManifest);

        // Procesar
        var result = await engine.ProcessRemoteManifestAsync();

        Assert.True(result.Success);
        Assert.Equal(1, result.AddedLocallyCount);
        Assert.False(result.HasConflicts);

        // Verificar persistencia local
        var localSnippet = await snippetRepo.GetByCodeAsync("!remoto");
        Assert.NotNull(localSnippet);
        Assert.Equal("Texto desde otra máquina", localSnippet.Content);

        var localFolder = await folderRepo.GetByIdAsync(remoteFolder.Id);
        Assert.NotNull(localFolder);
        Assert.Equal("RemotoFolder", localFolder.Name);
    }

    [Fact]
    public async Task ProcessRemoteManifestAsync_DetectsConcurrentModificationConflict()
    {
        var snippetRepo = new SnippetRepository(ConnectionFactory);
        var folderRepo = new FolderRepository(ConnectionFactory);
        var metadataRepo = new SyncMetadataRepository(ConnectionFactory);
        var syncStorage = new DiskSyncStorage(_tempSyncDir);

        // Registrar último sync en el pasado
        var lastSyncTime = DateTime.UtcNow.AddHours(-2);
        await metadataRepo.SaveAsync(new SyncMetadata("MY_MACHINE", lastSyncTime, "HASH_OLD"));

        // Atajo local modificado hace 1 hora (posterior al sync)
        var sharedSnippetId = Guid.NewGuid();
        var localSnippet = new Snippet(
            sharedSnippetId, "!edit", "Local Edit", "Contenido editado en LOCAL",
            null, false, true, false, false, null, 0, 0, null,
            lastSyncTime.AddHours(-1), DateTime.UtcNow.AddHours(-1));
        await snippetRepo.AddAsync(localSnippet);

        // Atajo remoto con mismo ID modificado hace 30 minutos (posterior al sync) con contenido diferente
        var remoteSnippet = new SyncSnippetDto(
            sharedSnippetId, "!edit", "Remote Edit", "Contenido editado en REMOTO",
            null, false, true, false, false, null, 0, 0, null,
            lastSyncTime.AddHours(-1), DateTime.UtcNow.AddMinutes(-30));

        var remoteManifest = new SyncManifestDto(
            1, "OTHER_MACHINE", DateTime.UtcNow,
            Array.Empty<SyncFolderDto>(), new[] { remoteSnippet }, Array.Empty<Guid>(), "HASH_NEW");

        await syncStorage.WriteManifestAsync(remoteManifest);

        var engine = new SyncEngine(snippetRepo, folderRepo, metadataRepo, syncStorage);
        var result = await engine.ProcessRemoteManifestAsync();

        Assert.True(result.Success);
        Assert.True(result.HasConflicts);
        Assert.Single(result.Conflicts);

        var conflict = result.Conflicts[0];
        Assert.Equal(SyncConflictType.ConcurrentModification, conflict.ConflictType);
        Assert.Equal("Contenido editado en LOCAL", conflict.LocalSnippet.Content);
        Assert.Equal("Contenido editado en REMOTO", conflict.RemoteSnippet.Content);
    }

    [Fact]
    public async Task ResolveConflictAsync_WithDuplicateBoth_CreatesAlternativeSnippet()
    {
        var snippetRepo = new SnippetRepository(ConnectionFactory);
        var folderRepo = new FolderRepository(ConnectionFactory);
        var metadataRepo = new SyncMetadataRepository(ConnectionFactory);
        var syncStorage = new DiskSyncStorage(_tempSyncDir);

        var id = Guid.NewGuid();
        var local = Snippet.Create("!dup", "Local", "Texto Local");
        var remote = new Snippet(id, "!dup", "Remoto", "Texto Remoto");

        var conflict = SyncConflict.Create(id, SyncConflictType.ConcurrentModification, local, remote);

        var engine = new SyncEngine(snippetRepo, folderRepo, metadataRepo, syncStorage);
        await snippetRepo.AddAsync(local);

        await engine.ResolveConflictAsync(conflict, SyncResolutionChoice.DuplicateBoth);

        // Debe mantenerse la versión local intacta y crearse un duplicado con sufijo
        var localSnippet = await snippetRepo.GetByCodeAsync("!dup");
        Assert.NotNull(localSnippet);
        Assert.Equal("Texto Local", localSnippet.Content);

        var duplicateSnippet = await snippetRepo.GetByCodeAsync("!dup_remoto");
        Assert.NotNull(duplicateSnippet);
        Assert.Equal("Texto Remoto", duplicateSnippet.Content);
    }
}
