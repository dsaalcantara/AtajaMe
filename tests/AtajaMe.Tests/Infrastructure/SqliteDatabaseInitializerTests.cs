using Dapper;

namespace AtajaMe.Tests.Infrastructure;

public sealed class SqliteDatabaseInitializerTests : SqliteTestBase
{
    [Fact]
    public async Task InitializeAsync_CreatesExpectedTables()
    {
        await using var connection = await ConnectionFactory.CreateOpenConnectionAsync();

        var tables = (await connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table';")).ToList();

        Assert.Contains("Folders", tables);
        Assert.Contains("Snippets", tables);
        Assert.Contains("ExcludedApplications", tables);
        Assert.Contains("AppSettings", tables);
        Assert.Contains("SyncMetadata", tables);
    }

    [Fact]
    public async Task InitializeAsync_CreatesExpectedIndexes()
    {
        await using var connection = await ConnectionFactory.CreateOpenConnectionAsync();

        var indexes = (await connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='index';")).ToList();

        Assert.Contains("IX_Snippets_Code", indexes);
        Assert.Contains("IX_Snippets_FolderId", indexes);
        Assert.Contains("IX_Snippets_IsFavorite", indexes);
        Assert.Contains("IX_Snippets_IsInTrash", indexes);
        Assert.Contains("IX_Snippets_LastUsedAt", indexes);
        Assert.Contains("IX_Folders_ParentId", indexes);
        Assert.Contains("IX_ExcludedApplications_ProcessName", indexes);
    }
}
