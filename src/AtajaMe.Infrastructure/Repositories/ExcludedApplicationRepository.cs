using System.Globalization;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Repositories;
using AtajaMe.Infrastructure.Persistence;
using Dapper;

namespace AtajaMe.Infrastructure.Repositories;

/// <summary>
/// Repositorio SQLite para la gestión de aplicaciones excluidas de expansión.
/// </summary>
public sealed class ExcludedApplicationRepository : IExcludedApplicationRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private volatile HashSet<string>? _excludedSetCache;

    public ExcludedApplicationRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void InvalidateCache()
    {
        _excludedSetCache = null;
    }

    public async Task<IReadOnlyList<ExcludedApplication>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT * FROM ExcludedApplications ORDER BY ProcessName COLLATE NOCASE;";
        var records = await connection.QueryAsync<ExcludedApplicationRecord>(sql);
        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task<bool> IsExcludedAsync(string processName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        var normalized = NormalizeProcessName(processName);
        var cache = await GetOrLoadExcludedSetAsync(cancellationToken);
        return cache.Contains(normalized);
    }

    private async Task<HashSet<string>> GetOrLoadExcludedSetAsync(CancellationToken cancellationToken)
    {
        var current = _excludedSetCache;
        if (current != null)
        {
            return current;
        }

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            current = _excludedSetCache;
            if (current != null)
            {
                return current;
            }

            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            const string sql = "SELECT ProcessName FROM ExcludedApplications;";
            var names = await connection.QueryAsync<string>(sql);

            var set = new HashSet<string>(names.Select(NormalizeProcessName), StringComparer.OrdinalIgnoreCase);
            _excludedSetCache = set;
            return set;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task AddAsync(ExcludedApplication application, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO ExcludedApplications (Id, ProcessName, Description, CreatedAt)
            VALUES (@Id, @ProcessName, @Description, @CreatedAt);";

        var record = ExcludedApplicationRecord.FromDomain(application);
        await connection.ExecuteAsync(sql, record);
        InvalidateCache();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM ExcludedApplications WHERE Id = @Id;";
        await connection.ExecuteAsync(sql, new { Id = id.ToString() });
        InvalidateCache();
    }

    private static string NormalizeProcessName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed[..^4];
        }
        return trimmed;
    }

    private sealed class ExcludedApplicationRecord
    {
        public string Id { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CreatedAt { get; set; } = string.Empty;

        public ExcludedApplication ToDomain() => new(
            Guid.Parse(Id),
            ProcessName,
            Description,
            DateTime.Parse(CreatedAt, null, DateTimeStyles.RoundtripKind)
        );

        public static ExcludedApplicationRecord FromDomain(ExcludedApplication app) => new()
        {
            Id = app.Id.ToString(),
            ProcessName = app.ProcessName,
            Description = app.Description,
            CreatedAt = app.CreatedAt.ToString("o", CultureInfo.InvariantCulture)
        };
    }
}
