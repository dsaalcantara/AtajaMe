using AtajaMe.Domain.Entities;

namespace AtajaMe.Domain.Repositories;

/// <summary>
/// Contrato de persistencia para la lista de aplicaciones excluidas.
/// </summary>
public interface IExcludedApplicationRepository
{
    Task<IReadOnlyList<ExcludedApplication>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> IsExcludedAsync(string processName, CancellationToken cancellationToken = default);
    Task AddAsync(ExcludedApplication application, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
