using AtajaMe.Domain.Entities;

namespace AtajaMe.Domain.Repositories;

/// <summary>
/// Contrato de persistencia para la gestión de atajos de texto.
/// </summary>
public interface ISnippetRepository
{
    Task<IReadOnlyList<Snippet>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Snippet>> GetByFolderIdAsync(Guid? folderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Snippet>> GetFavoritesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Snippet>> GetRecentAsync(int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Snippet>> GetTrashAsync(CancellationToken cancellationToken = default);
    Task<Snippet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Snippet?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Snippet>> SearchAsync(string query, Guid? folderId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Snippet snippet, CancellationToken cancellationToken = default);
    Task UpdateAsync(Snippet snippet, CancellationToken cancellationToken = default);
    Task DeletePermanentAsync(Guid id, CancellationToken cancellationToken = default);
    Task PurgeTrashOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
    Task<bool> ExistsCodeAsync(string code, Guid? excludeSnippetId = null, CancellationToken cancellationToken = default);
}
