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

    public ExcludedApplicationRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
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
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(1) FROM ExcludedApplications WHERE ProcessName = @ProcessName COLLATE NOCASE;";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { ProcessName = normalized });
        return count > 0;
    }

    public async Task AddAsync(ExcludedApplication application, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO ExcludedApplications (Id, ProcessName, Description, CreatedAt)
            VALUES (@Id, @ProcessName, @Description, @CreatedAt);";

        var record = ExcludedApplicationRecord.FromDomain(application);
        await connection.ExecuteAsync(sql, record);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM ExcludedApplications WHERE Id = @Id;";
        await connection.ExecuteAsync(sql, new { Id = id.ToString() });
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
