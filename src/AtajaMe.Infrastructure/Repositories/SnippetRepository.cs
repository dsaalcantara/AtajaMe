using System.Globalization;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Repositories;
using AtajaMe.Infrastructure.Persistence;
using Dapper;

namespace AtajaMe.Infrastructure.Repositories;

/// <summary>
/// Repositorio SQLite de alta velocidad para la gestión de atajos de texto.
/// </summary>
public sealed class SnippetRepository : ISnippetRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public SnippetRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Snippet>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT * FROM Snippets 
            WHERE IsInTrash = 0 AND IsActive = 1 
            ORDER BY SortOrder, Name COLLATE NOCASE";

        var records = await connection.QueryAsync<SnippetRecord>(sql);
        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<Snippet>> GetByFolderIdAsync(Guid? folderId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        string sql;
        object parameters;

        if (folderId.HasValue)
        {
            sql = "SELECT * FROM Snippets WHERE FolderId = @FolderId AND IsInTrash = 0 ORDER BY SortOrder, Name COLLATE NOCASE";
            parameters = new { FolderId = folderId.Value.ToString() };
        }
        else
        {
            sql = "SELECT * FROM Snippets WHERE FolderId IS NULL AND IsInTrash = 0 ORDER BY SortOrder, Name COLLATE NOCASE";
            parameters = new { };
        }

        var records = await connection.QueryAsync<SnippetRecord>(sql, parameters);
        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<Snippet>> GetFavoritesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT * FROM Snippets 
            WHERE IsFavorite = 1 AND IsInTrash = 0 
            ORDER BY SortOrder, Name COLLATE NOCASE";

        var records = await connection.QueryAsync<SnippetRecord>(sql);
        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<Snippet>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT * FROM Snippets 
            WHERE LastUsedAt IS NOT NULL AND IsInTrash = 0 
            ORDER BY LastUsedAt DESC 
            LIMIT @Limit";

        var records = await connection.QueryAsync<SnippetRecord>(sql, new { Limit = limit });
        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<Snippet>> GetTrashAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT * FROM Snippets 
            WHERE IsInTrash = 1 
            ORDER BY DeletedAt DESC";

        var records = await connection.QueryAsync<SnippetRecord>(sql);
        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task<Snippet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT * FROM Snippets WHERE Id = @Id LIMIT 1";

        var record = await connection.QuerySingleOrDefaultAsync<SnippetRecord>(sql, new { Id = id.ToString() });
        return record?.ToDomain();
    }

    public async Task<Snippet?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT * FROM Snippets WHERE Code = @Code COLLATE NOCASE AND IsInTrash = 0 LIMIT 1";

        var record = await connection.QuerySingleOrDefaultAsync<SnippetRecord>(sql, new { Code = code.Trim() });
        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<Snippet>> SearchAsync(string query, Guid? folderId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var trimmedQuery = query.Trim();
        var pattern = $"%{trimmedQuery}%";

        string sql;
        object parameters;

        if (folderId.HasValue)
        {
            sql = @"
                SELECT * FROM Snippets 
                WHERE IsInTrash = 0 
                  AND FolderId = @FolderId 
                  AND (Code LIKE @Pattern OR Name LIKE @Pattern OR Content LIKE @Pattern)
                ORDER BY Name COLLATE NOCASE";
            parameters = new { FolderId = folderId.Value.ToString(), Pattern = pattern };
        }
        else
        {
            sql = @"
                SELECT * FROM Snippets 
                WHERE IsInTrash = 0 
                  AND (Code LIKE @Pattern OR Name LIKE @Pattern OR Content LIKE @Pattern)
                ORDER BY Name COLLATE NOCASE";
            parameters = new { Pattern = pattern };
        }

        var records = await connection.QueryAsync<SnippetRecord>(sql, parameters);
        return records.Select(r => r.ToDomain()).ToList();
    }

    public async Task AddAsync(Snippet snippet, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO Snippets (
                Id, FolderId, Code, Name, Content, CaseSensitive, 
                IsActive, IsFavorite, IsInTrash, DeletedAt, 
                SortOrder, UsageCount, LastUsedAt, CreatedAt, UpdatedAt
            ) VALUES (
                @Id, @FolderId, @Code, @Name, @Content, @CaseSensitive, 
                @IsActive, @IsFavorite, @IsInTrash, @DeletedAt, 
                @SortOrder, @UsageCount, @LastUsedAt, @CreatedAt, @UpdatedAt
            );";

        var record = SnippetRecord.FromDomain(snippet);
        await connection.ExecuteAsync(sql, record);
    }

    public async Task UpdateAsync(Snippet snippet, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE Snippets SET 
                FolderId = @FolderId,
                Code = @Code,
                Name = @Name,
                Content = @Content,
                CaseSensitive = @CaseSensitive,
                IsActive = @IsActive,
                IsFavorite = @IsFavorite,
                IsInTrash = @IsInTrash,
                DeletedAt = @DeletedAt,
                SortOrder = @SortOrder,
                UsageCount = @UsageCount,
                LastUsedAt = @LastUsedAt,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;";

        var record = SnippetRecord.FromDomain(snippet);
        await connection.ExecuteAsync(sql, record);
    }

    public async Task DeletePermanentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM Snippets WHERE Id = @Id;";
        await connection.ExecuteAsync(sql, new { Id = id.ToString() });
    }

    public async Task PurgeTrashOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM Snippets WHERE IsInTrash = 1 AND DeletedAt <= @CutoffDate;";
        var cutoffIso = cutoffDate.ToString("o", CultureInfo.InvariantCulture);
        await connection.ExecuteAsync(sql, new { CutoffDate = cutoffIso });
    }

    public async Task<bool> ExistsCodeAsync(string code, Guid? excludeSnippetId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        string sql;
        object parameters;

        if (excludeSnippetId.HasValue)
        {
            sql = "SELECT COUNT(1) FROM Snippets WHERE Code = @Code COLLATE NOCASE AND Id != @ExcludeId;";
            parameters = new { Code = code.Trim(), ExcludeId = excludeSnippetId.Value.ToString() };
        }
        else
        {
            sql = "SELECT COUNT(1) FROM Snippets WHERE Code = @Code COLLATE NOCASE;";
            parameters = new { Code = code.Trim() };
        }

        var count = await connection.ExecuteScalarAsync<int>(sql, parameters);
        return count > 0;
    }

    private sealed class SnippetRecord
    {
        public string Id { get; set; } = string.Empty;
        public string? FolderId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public long CaseSensitive { get; set; }
        public long IsActive { get; set; }
        public long IsFavorite { get; set; }
        public long IsInTrash { get; set; }
        public string? DeletedAt { get; set; }
        public long SortOrder { get; set; }
        public long UsageCount { get; set; }
        public string? LastUsedAt { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;

        public Snippet ToDomain() => new(
            Guid.Parse(Id),
            Code,
            Name,
            Content,
            FolderId != null ? Guid.Parse(FolderId) : null,
            CaseSensitive == 1,
            IsActive == 1,
            IsFavorite == 1,
            IsInTrash == 1,
            DeletedAt != null ? DateTime.Parse(DeletedAt, null, DateTimeStyles.RoundtripKind) : null,
            (int)SortOrder,
            (int)UsageCount,
            LastUsedAt != null ? DateTime.Parse(LastUsedAt, null, DateTimeStyles.RoundtripKind) : null,
            DateTime.Parse(CreatedAt, null, DateTimeStyles.RoundtripKind),
            DateTime.Parse(UpdatedAt, null, DateTimeStyles.RoundtripKind)
        );

        public static SnippetRecord FromDomain(Snippet s) => new()
        {
            Id = s.Id.ToString(),
            FolderId = s.FolderId?.ToString(),
            Code = s.Code,
            Name = s.Name,
            Content = s.Content,
            CaseSensitive = s.CaseSensitive ? 1 : 0,
            IsActive = s.IsActive ? 1 : 0,
            IsFavorite = s.IsFavorite ? 1 : 0,
            IsInTrash = s.IsInTrash ? 1 : 0,
            DeletedAt = s.DeletedAt?.ToString("o", CultureInfo.InvariantCulture),
            SortOrder = s.SortOrder,
            UsageCount = s.UsageCount,
            LastUsedAt = s.LastUsedAt?.ToString("o", CultureInfo.InvariantCulture),
            CreatedAt = s.CreatedAt.ToString("o", CultureInfo.InvariantCulture),
            UpdatedAt = s.UpdatedAt.ToString("o", CultureInfo.InvariantCulture)
        };
    }
}
