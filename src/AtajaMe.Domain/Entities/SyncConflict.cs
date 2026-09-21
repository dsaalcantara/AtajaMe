using AtajaMe.Domain.Enums;

namespace AtajaMe.Domain.Entities;

/// <summary>
/// Representa una discrepancia o conflicto entre la versión local y remota de un atajo (RF-24).
/// </summary>
public sealed class SyncConflict
{
    public Guid Id { get; }
    public Guid SnippetId { get; }
    public SyncConflictType ConflictType { get; }
    public Snippet LocalSnippet { get; }
    public Snippet RemoteSnippet { get; }
    public DateTime DetectedAt { get; }
    public DateTime? ResolvedAt { get; private set; }
    public SyncResolutionChoice? Resolution { get; private set; }

    public bool IsResolved => ResolvedAt.HasValue;

    public SyncConflict(
        Guid id,
        Guid snippetId,
        SyncConflictType conflictType,
        Snippet localSnippet,
        Snippet remoteSnippet,
        DateTime? detectedAt = null,
        DateTime? resolvedAt = null,
        SyncResolutionChoice? resolution = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SnippetId = snippetId;
        ConflictType = conflictType;
        LocalSnippet = localSnippet ?? throw new ArgumentNullException(nameof(localSnippet));
        RemoteSnippet = remoteSnippet ?? throw new ArgumentNullException(nameof(remoteSnippet));
        DetectedAt = detectedAt ?? DateTime.UtcNow;
        ResolvedAt = resolvedAt;
        Resolution = resolution;
    }

    public static SyncConflict Create(
        Guid snippetId,
        SyncConflictType conflictType,
        Snippet localSnippet,
        Snippet remoteSnippet)
    {
        return new SyncConflict(
            Guid.NewGuid(),
            snippetId,
            conflictType,
            localSnippet,
            remoteSnippet);
    }

    public void Resolve(SyncResolutionChoice choice)
    {
        Resolution = choice;
        ResolvedAt = DateTime.UtcNow;
    }
}
