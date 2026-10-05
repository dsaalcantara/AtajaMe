using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AtajaMe.Application.Sync.DTOs;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Enums;
using AtajaMe.Domain.Repositories;

namespace AtajaMe.Application.Sync.Services;

/// <summary>
/// Motor central de sincronización desacoplada, detección y resolución de conflictos (RF-22, RF-23, RF-24).
/// </summary>
public sealed class SyncEngine : ISyncEngine
{
    private readonly ISnippetRepository _snippetRepository;
    private readonly IFolderRepository _folderRepository;
    private readonly ISyncMetadataRepository _syncMetadataRepository;
    private readonly ISyncStorage _syncStorage;

    private readonly List<SyncConflict> _pendingConflicts = new();
    private readonly object _lock = new();

    public SyncEngine(
        ISnippetRepository snippetRepository,
        IFolderRepository folderRepository,
        ISyncMetadataRepository syncMetadataRepository,
        ISyncStorage syncStorage)
    {
        _snippetRepository = snippetRepository ?? throw new ArgumentNullException(nameof(snippetRepository));
        _folderRepository = folderRepository ?? throw new ArgumentNullException(nameof(folderRepository));
        _syncMetadataRepository = syncMetadataRepository ?? throw new ArgumentNullException(nameof(syncMetadataRepository));
        _syncStorage = syncStorage ?? throw new ArgumentNullException(nameof(syncStorage));
    }

    public async Task<SyncManifestDto> ExportManifestAsync(CancellationToken cancellationToken = default)
    {
        if (!_syncStorage.IsAvailable())
        {
            throw new InvalidOperationException("La carpeta compartida de sincronización no está disponible.");
        }

        var metadata = await _syncMetadataRepository.GetAsync(cancellationToken) ?? SyncMetadata.CreateDefault();

        var localFolders = await _folderRepository.GetAllAsync(cancellationToken);
        var librarySnippets = await _snippetRepository.GetAllAsync(cancellationToken);
        var trashSnippets = await _snippetRepository.GetTrashAsync(cancellationToken);

        var allSnippets = librarySnippets.Concat(trashSnippets).DistinctBy(s => s.Id).ToList();

        var folderDtos = localFolders.Select(f => new SyncFolderDto(
            f.Id,
            f.Name,
            f.ParentId,
            f.SortOrder,
            f.CreatedAt,
            f.UpdatedAt)).OrderBy(f => f.Id).ToList();

        var snippetDtos = allSnippets.Select(s => new SyncSnippetDto(
            s.Id,
            s.Code,
            s.Name,
            s.Content,
            s.FolderId,
            s.CaseSensitive,
            s.IsActive,
            s.IsFavorite,
            s.IsInTrash,
            s.DeletedAt,
            s.SortOrder,
            s.UsageCount,
            s.LastUsedAt,
            s.CreatedAt,
            s.UpdatedAt)).OrderBy(s => s.Id).ToList();

        var now = DateTime.UtcNow;
        var hash = ComputeManifestHash(metadata.MachineId, folderDtos, snippetDtos);

        var manifest = new SyncManifestDto(
            Version: 1,
            MachineId: metadata.MachineId,
            TimestampUtc: now,
            Folders: folderDtos,
            Snippets: snippetDtos,
            DeletedSnippetIds: Array.Empty<Guid>(),
            ManifestHash: hash);

        await _syncStorage.WriteManifestAsync(manifest, cancellationToken);

        metadata.Update(now, hash);
        await _syncMetadataRepository.SaveAsync(metadata, cancellationToken);

        return manifest;
    }

    public async Task<SyncResult> ProcessRemoteManifestAsync(CancellationToken cancellationToken = default)
    {
        // Tolerancia a fallos cuando no está disponible la carpeta remota (RF-23)
        if (!_syncStorage.IsAvailable())
        {
            return SyncResult.Unavailable("La carpeta remota no está accesible actualmente.");
        }

        SyncManifestDto? remoteManifest;
        try
        {
            remoteManifest = await _syncStorage.ReadManifestAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return SyncResult.Failed($"Error al leer el manifiesto remoto: {ex.Message}");
        }

        if (remoteManifest == null)
        {
            return SyncResult.Succeeded(0, 0, "No se encontró ningún paquete de sincronización remoto.");
        }

        var metadata = await _syncMetadataRepository.GetAsync(cancellationToken) ?? SyncMetadata.CreateDefault();

        // Si fue emitido por esta misma máquina, no hay cambios externos
        if (string.Equals(remoteManifest.MachineId, metadata.MachineId, StringComparison.OrdinalIgnoreCase))
        {
            return SyncResult.Succeeded(0, 0, "El manifiesto remoto fue emitido por este mismo equipo.");
        }

        // Si el hash coincide con el último procesado, ya estamos sincronizados
        if (string.Equals(remoteManifest.ManifestHash, metadata.LastSyncHash, StringComparison.OrdinalIgnoreCase))
        {
            return SyncResult.Succeeded(0, 0, "Sin cambios remotos respecto a la última sincronización.");
        }

        // 1. Sincronizar Carpetas
        await SyncFoldersAsync(remoteManifest.Folders, cancellationToken);

        // 2. Sincronizar Atajos
        int addedCount = 0;
        int updatedCount = 0;
        var detectedConflicts = new List<SyncConflict>();

        foreach (var rs in remoteManifest.Snippets)
        {
            var (outcome, conflict) = await ProcessRemoteSnippetAsync(rs, metadata, cancellationToken);
            switch (outcome)
            {
                case SnippetSyncOutcome.Added:
                    addedCount++;
                    break;
                case SnippetSyncOutcome.Updated:
                    updatedCount++;
                    break;
                case SnippetSyncOutcome.Conflict when conflict != null:
                    detectedConflicts.Add(conflict);
                    break;
            }
        }

        lock (_lock)
        {
            _pendingConflicts.Clear();
            _pendingConflicts.AddRange(detectedConflicts);
        }

        if (detectedConflicts.Count > 0)
        {
            return SyncResult.WithConflicts(detectedConflicts, addedCount, updatedCount);
        }

        // Sin conflictos: actualizar metadatos locales
        metadata.Update(remoteManifest.TimestampUtc, remoteManifest.ManifestHash);
        await _syncMetadataRepository.SaveAsync(metadata, cancellationToken);

        return SyncResult.Succeeded(addedCount, updatedCount, "Sincronización completada con éxito.");
    }

    private enum SnippetSyncOutcome { Unchanged, Added, Updated, Conflict }

    private async Task SyncFoldersAsync(IReadOnlyList<SyncFolderDto> remoteFolders, CancellationToken cancellationToken)
    {
        var localFolders = (await _folderRepository.GetAllAsync(cancellationToken)).ToDictionary(f => f.Id);
        foreach (var rf in remoteFolders)
        {
            if (!localFolders.TryGetValue(rf.Id, out var localFolder))
            {
                var newFolder = new Folder(rf.Id, rf.Name, rf.ParentId, rf.SortOrder, rf.CreatedAtUtc, rf.UpdatedAtUtc);
                await _folderRepository.AddAsync(newFolder, cancellationToken);
                localFolders[newFolder.Id] = newFolder;
            }
            else if (rf.UpdatedAtUtc > localFolder.UpdatedAt)
            {
                localFolder.UpdateName(rf.Name);
                localFolder.MoveTo(rf.ParentId);
                localFolder.SetSortOrder(rf.SortOrder);
                await _folderRepository.UpdateAsync(localFolder, cancellationToken);
            }
        }
    }

    private async Task<(SnippetSyncOutcome Outcome, SyncConflict? Conflict)> ProcessRemoteSnippetAsync(
        SyncSnippetDto rs,
        SyncMetadata metadata,
        CancellationToken cancellationToken)
    {
        var remoteSnippetEntity = MapToSnippetEntity(rs);
        var localSnippet = await _snippetRepository.GetByIdAsync(rs.Id, cancellationToken);

        if (localSnippet == null)
        {
            return await HandleNewRemoteSnippetAsync(rs, remoteSnippetEntity, cancellationToken);
        }

        return await HandleExistingSnippetComparisonAsync(localSnippet, remoteSnippetEntity, rs, metadata, cancellationToken);
    }

    private async Task<(SnippetSyncOutcome Outcome, SyncConflict? Conflict)> HandleNewRemoteSnippetAsync(
        SyncSnippetDto rs,
        Snippet remoteSnippetEntity,
        CancellationToken cancellationToken)
    {
        var existingWithCode = await _snippetRepository.GetByCodeAsync(rs.Code, cancellationToken);
        if (existingWithCode != null)
        {
            var conflict = SyncConflict.Create(rs.Id, SyncConflictType.CodeCollision, existingWithCode, remoteSnippetEntity);
            return (SnippetSyncOutcome.Conflict, conflict);
        }

        await _snippetRepository.AddAsync(remoteSnippetEntity, cancellationToken);
        return (SnippetSyncOutcome.Added, null);
    }

    private async Task<(SnippetSyncOutcome Outcome, SyncConflict? Conflict)> HandleExistingSnippetComparisonAsync(
        Snippet localSnippet,
        Snippet remoteSnippetEntity,
        SyncSnippetDto rs,
        SyncMetadata metadata,
        CancellationToken cancellationToken)
    {
        if (AreSnippetsEquivalent(localSnippet, remoteSnippetEntity))
        {
            return (SnippetSyncOutcome.Unchanged, null);
        }

        bool localChanged = localSnippet.UpdatedAt > metadata.LastSyncTimestamp;
        bool remoteChanged = rs.UpdatedAtUtc > metadata.LastSyncTimestamp;

        if (remoteChanged && !localChanged)
        {
            await ApplyRemoteSnippetToLocalAsync(localSnippet, remoteSnippetEntity, cancellationToken);
            return (SnippetSyncOutcome.Updated, null);
        }

        if (!remoteChanged && localChanged)
        {
            return (SnippetSyncOutcome.Unchanged, null);
        }

        var conflict = SyncConflict.Create(rs.Id, SyncConflictType.ConcurrentModification, localSnippet, remoteSnippetEntity);
        return (SnippetSyncOutcome.Conflict, conflict);
    }

    private static Snippet MapToSnippetEntity(SyncSnippetDto rs)
    {
        return new Snippet(
            rs.Id,
            rs.Code,
            rs.Name,
            rs.Content,
            rs.FolderId,
            rs.CaseSensitive,
            rs.IsActive,
            rs.IsFavorite,
            rs.IsInTrash,
            rs.DeletedAtUtc,
            rs.SortOrder,
            rs.UsageCount,
            rs.LastUsedAtUtc,
            rs.CreatedAtUtc,
            rs.UpdatedAtUtc);
    }

    public async Task ResolveConflictAsync(SyncConflict conflict, SyncResolutionChoice choice, CancellationToken cancellationToken = default)
    {
        if (conflict == null)
        {
            throw new ArgumentNullException(nameof(conflict));
        }

        conflict.Resolve(choice);

        switch (choice)
        {
            case SyncResolutionChoice.KeepLocal:
                // No se modifican los datos locales; el siguiente ciclo de exportación sobrescribirá el manifiesto
                break;

            case SyncResolutionChoice.KeepRemote:
                var existing = await _snippetRepository.GetByIdAsync(conflict.SnippetId, cancellationToken);
                if (existing != null)
                {
                    await ApplyRemoteSnippetToLocalAsync(existing, conflict.RemoteSnippet, cancellationToken);
                }
                else
                {
                    await _snippetRepository.AddAsync(conflict.RemoteSnippet, cancellationToken);
                }
                break;

            case SyncResolutionChoice.DuplicateBoth:
                // Conservar la versión local intacta y crear un nuevo atajo con la versión remota
                var uniqueCode = await GenerateUniqueCodeAsync(conflict.RemoteSnippet.Code, cancellationToken);
                var duplicate = Snippet.Create(
                    uniqueCode,
                    $"{conflict.RemoteSnippet.Name} (Remoto)",
                    conflict.RemoteSnippet.Content,
                    conflict.RemoteSnippet.FolderId,
                    conflict.RemoteSnippet.CaseSensitive);

                if (!conflict.RemoteSnippet.IsActive)
                {
                    duplicate.ToggleActive();
                }
                if (conflict.RemoteSnippet.IsFavorite)
                {
                    duplicate.ToggleFavorite();
                }

                await _snippetRepository.AddAsync(duplicate, cancellationToken);
                break;
        }

        lock (_lock)
        {
            _pendingConflicts.RemoveAll(c => c.SnippetId == conflict.SnippetId);
        }
    }

    public Task<IReadOnlyList<SyncConflict>> GetPendingConflictsAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<SyncConflict>>(_pendingConflicts.ToList());
        }
    }

    private async Task ApplyRemoteSnippetToLocalAsync(Snippet local, Snippet remote, CancellationToken cancellationToken)
    {
        local.Update(
            remote.Code,
            remote.Name,
            remote.Content,
            remote.FolderId,
            remote.CaseSensitive);

        if (remote.IsActive != local.IsActive)
        {
            local.ToggleActive();
        }
        if (remote.IsFavorite != local.IsFavorite)
        {
            local.ToggleFavorite();
        }
        if (remote.IsInTrash && !local.IsInTrash)
        {
            local.MoveToTrash();
        }
        else if (!remote.IsInTrash && local.IsInTrash)
        {
            local.RestoreFromTrash();
        }

        await _snippetRepository.UpdateAsync(local, cancellationToken);
    }

    private static bool AreSnippetsEquivalent(Snippet a, Snippet b)
    {
        return a.Code == b.Code &&
               a.Name == b.Name &&
               a.Content == b.Content &&
               a.FolderId == b.FolderId &&
               a.CaseSensitive == b.CaseSensitive &&
               a.IsActive == b.IsActive &&
               a.IsFavorite == b.IsFavorite &&
               a.IsInTrash == b.IsInTrash;
    }

    private async Task<string> GenerateUniqueCodeAsync(string baseCode, CancellationToken cancellationToken)
    {
        int counter = 1;
        while (true)
        {
            string suffix = counter == 1 ? "_remoto" : $"_remoto_{counter}";
            string candidate = $"{baseCode}{suffix}";
            if (candidate.Length > Snippet.MaxCodeLength)
            {
                candidate = candidate.Substring(0, Snippet.MaxCodeLength);
            }

            bool exists = await _snippetRepository.ExistsCodeAsync(candidate, null, cancellationToken);
            if (!exists)
            {
                return candidate;
            }

            counter++;
        }
    }

    private static string ComputeManifestHash(string machineId, IReadOnlyList<SyncFolderDto> folders, IReadOnlyList<SyncSnippetDto> snippets)
    {
        using var sha256 = SHA256.Create();
        var json = JsonSerializer.Serialize(new { machineId, folders, snippets });
        var bytes = Encoding.UTF8.GetBytes(json);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes);
    }
}
