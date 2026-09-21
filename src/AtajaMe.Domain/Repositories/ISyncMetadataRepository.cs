using AtajaMe.Domain.Entities;

namespace AtajaMe.Domain.Repositories;

/// <summary>
/// Contrato de persistencia para los metadatos de sincronización de la máquina.
/// </summary>
public interface ISyncMetadataRepository
{
    Task<SyncMetadata?> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(SyncMetadata metadata, CancellationToken cancellationToken = default);
}
