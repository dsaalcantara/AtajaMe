using AtajaMe.Domain.Entities;

namespace AtajaMe.Application.Sync.DTOs;

/// <summary>
/// DTO de carpeta serializable en el paquete de sincronización.
/// </summary>
public sealed record SyncFolderDto(
    Guid Id,
    string Name,
    Guid? ParentId,
    int SortOrder,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

/// <summary>
/// DTO de atajo de texto serializable en el paquete de sincronización.
/// </summary>
public sealed record SyncSnippetDto(
    Guid Id,
    string Code,
    string Name,
    string Content,
    Guid? FolderId,
    bool CaseSensitive,
    bool IsActive,
    bool IsFavorite,
    bool IsInTrash,
    DateTime? DeletedAtUtc,
    int SortOrder,
    int UsageCount,
    DateTime? LastUsedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

/// <summary>
/// Paquete de intercambio desacoplado (sync_manifest.json) para sincronización en la nube (RF-22, RF-24).
/// </summary>
public sealed record SyncManifestDto(
    int Version,
    string MachineId,
    DateTime TimestampUtc,
    IReadOnlyList<SyncFolderDto> Folders,
    IReadOnlyList<SyncSnippetDto> Snippets,
    IReadOnlyList<Guid> DeletedSnippetIds,
    string ManifestHash);

/// <summary>
/// Resultado de un ciclo de sincronización (RF-23, RF-24).
/// </summary>
public sealed record SyncResult(
    bool Success,
    int AddedLocallyCount,
    int UpdatedLocallyCount,
    IReadOnlyList<SyncConflict> Conflicts,
    string? Message = null)
{
    public bool HasConflicts => Conflicts.Count > 0;

    public static SyncResult Succeeded(int added = 0, int updated = 0, string? message = null)
        => new SyncResult(true, added, updated, Array.Empty<SyncConflict>(), message);

    public static SyncResult WithConflicts(IReadOnlyList<SyncConflict> conflicts, int added = 0, int updated = 0)
        => new SyncResult(true, added, updated, conflicts, $"{conflicts.Count} conflicto(s) detectado(s).");

    public static SyncResult Unavailable(string reason)
        => new SyncResult(false, 0, 0, Array.Empty<SyncConflict>(), $"Ubicación no disponible: {reason}");

    public static SyncResult Failed(string error)
        => new SyncResult(false, 0, 0, Array.Empty<SyncConflict>(), error);
}
