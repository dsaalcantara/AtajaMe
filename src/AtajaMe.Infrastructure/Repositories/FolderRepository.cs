using System.Globalization;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Repositories;
using AtajaMe.Infrastructure.Persistence;
using Dapper;

namespace AtajaMe.Infrastructure.Repositories;

/// <summary>
/// Repositorio SQLite para la gestión jerárquica de carpetas de atajos.
/// </summary>
public sealed class FolderRepository : IFolderRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public FolderRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Folder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT * FROM Folders ORDER BY SortOrder, Name COLLATE NOCASE;";
        var records = await connection.QueryAsync<FolderRecord>(sql);
        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task<Folder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT * FROM Folders WHERE Id = @Id LIMIT 1;";
        var record = await connection.QuerySingleOrDefaultAsync<FolderRecord>(sql, new { Id = id.ToString() });
        return record?.ToDomain();
    }

    public async Task<int> GetDepthAsync(Guid folderId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = @"
            WITH RECURSIVE FolderHierarchy(Id, ParentId, Depth) AS (
                SELECT Id, ParentId, 1
                FROM Folders
                WHERE Id = @FolderId
                UNION ALL
                SELECT f.Id, f.ParentId, fh.Depth + 1
                FROM Folders f
                JOIN FolderHierarchy fh ON f.Id = fh.ParentId
            )
            SELECT COALESCE(MAX(Depth), 0) FROM FolderHierarchy;";

        return await connection.ExecuteScalarAsync<int>(sql, new { FolderId = folderId.ToString() });
    }

    public async Task AddAsync(Folder folder, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO Folders (Id, ParentId, Name, SortOrder, CreatedAt, UpdatedAt)
            VALUES (@Id, @ParentId, @Name, @SortOrder, @CreatedAt, @UpdatedAt);";

        var record = FolderRecord.FromDomain(folder);
        await connection.ExecuteAsync(sql, record);
    }

    public async Task UpdateAsync(Folder folder, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE Folders SET
                ParentId = @ParentId,
                Name = @Name,
                SortOrder = @SortOrder,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;";

        var record = FolderRecord.FromDomain(folder);
        await connection.ExecuteAsync(sql, record);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM Folders WHERE Id = @Id;";
        await connection.ExecuteAsync(sql, new { Id = id.ToString() });
    }

    private sealed class FolderRecord
    {
        public string Id { get; set; } = string.Empty;
        public string? ParentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public long SortOrder { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;

        public Folder ToDomain() => new(
            Guid.Parse(Id),
            Name,
            ParentId != null ? Guid.Parse(ParentId) : null,
            (int)SortOrder,
            DateTime.Parse(CreatedAt, null, DateTimeStyles.RoundtripKind),
            DateTime.Parse(UpdatedAt, null, DateTimeStyles.RoundtripKind)
        );

        public static FolderRecord FromDomain(Folder f) => new()
        {
            Id = f.Id.ToString(),
            ParentId = f.ParentId?.ToString(),
            Name = f.Name,
            SortOrder = f.SortOrder,
            CreatedAt = f.CreatedAt.ToString("o", CultureInfo.InvariantCulture),
            UpdatedAt = f.UpdatedAt.ToString("o", CultureInfo.InvariantCulture)
        };
    }
}
