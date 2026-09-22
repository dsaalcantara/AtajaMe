using AtajaMe.Domain.Repositories;
using AtajaMe.Infrastructure.Persistence;
using Dapper;

namespace AtajaMe.Infrastructure.Repositories;

/// <summary>
/// Repositorio SQLite para persistir las configuraciones clave-valor de la aplicación en la tabla AppSettings.
/// </summary>
public sealed class AppSettingsRepository : IAppSettingsRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public AppSettingsRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT Value FROM AppSettings WHERE Key = @Key;";
        return await connection.QueryFirstOrDefaultAsync<string?>(sql, new { Key = key });
    }

    public async Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO AppSettings (Key, Value)
            VALUES (@Key, @Value)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;";
        await connection.ExecuteAsync(sql, new { Key = key, Value = value });
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT Key, Value FROM AppSettings;";
        var rows = await connection.QueryAsync<(string Key, string Value)>(sql);
        return rows.ToDictionary(r => r.Key, r => r.Value);
    }

    public async Task DeleteKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM AppSettings WHERE Key = @Key;";
        await connection.ExecuteAsync(sql, new { Key = key });
    }
}
