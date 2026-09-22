namespace AtajaMe.Domain.Repositories;

/// <summary>
/// Contrato del repositorio para la persistencia de configuraciones de la aplicación (AppSettings).
/// </summary>
public interface IAppSettingsRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default);
    Task DeleteKeyAsync(string key, CancellationToken cancellationToken = default);
}
