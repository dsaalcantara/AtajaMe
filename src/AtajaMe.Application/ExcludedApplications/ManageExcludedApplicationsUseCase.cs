using AtajaMe.Application.DTOs;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Repositories;

namespace AtajaMe.Application.ExcludedApplications;

/// <summary>
/// Caso de uso para la gestión y consulta de aplicaciones excluidas de expansión.
/// </summary>
public sealed class ManageExcludedApplicationsUseCase
{
    private readonly IExcludedApplicationRepository _repository;

    public ManageExcludedApplicationsUseCase(IExcludedApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ExcludedApplicationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var apps = await _repository.GetAllAsync(cancellationToken);
        return apps.Select(ExcludedApplicationDto.FromDomain).ToList();
    }

    public async Task<bool> IsExcludedAsync(string processName, CancellationToken cancellationToken = default)
    {
        return await _repository.IsExcludedAsync(processName, cancellationToken);
    }

    public async Task<ExcludedApplicationDto> AddAsync(string processName, string? description = null, CancellationToken cancellationToken = default)
    {
        var app = ExcludedApplication.Create(processName, description);
        await _repository.AddAsync(app, cancellationToken);
        return ExcludedApplicationDto.FromDomain(app);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(id, cancellationToken);
    }
}
