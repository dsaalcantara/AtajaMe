using AtajaMe.Domain.Entities;

namespace AtajaMe.Domain.Repositories;

/// <summary>
/// Contrato de persistencia para la organización de carpetas.
/// </summary>
public interface IFolderRepository
{
    Task<IReadOnlyList<Folder>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Folder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetDepthAsync(Guid folderId, CancellationToken cancellationToken = default);

    Task AddAsync(Folder folder, CancellationToken cancellationToken = default);
    Task UpdateAsync(Folder folder, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
