using AtajaMe.Application.Expansion;
using AtajaMe.Application.Variables;
using AtajaMe.Domain.Entities;
using AtajaMe.Infrastructure.Repositories;
using AtajaMe.Tests.Infrastructure;

namespace AtajaMe.Tests.Expansion;

public class SnippetExpansionEngineSqliteIntegrationTests : SqliteTestBase
{
    [Fact]
    public async Task ProcessKeyAsync_WithSqliteRepository_ExpandsCorrectly()
    {
        var repository = new SnippetRepository(ConnectionFactory);
        var variableService = new VariableResolutionService();
        using var engine = new SnippetExpansionEngine(repository, variableService);

        var snippet = Snippet.Create("sig", "Firma", "Atentamente,\nEquipo AtajaMe");
        await repository.AddAsync(snippet);

        foreach (var c in "sig")
        {
            await engine.ProcessKeyAsync(c);
        }

        var result = await engine.ProcessKeyAsync(' ');

        Assert.Equal(ExpansionStatus.Success, result.Status);
        Assert.Equal("Atentamente,\nEquipo AtajaMe", result.ExpandedContent);
        Assert.Equal(4, result.CharactersToDelete); // "sig" (3) + ' ' (1) = 4

        // Registrar uso en SQLite
        await engine.RecordExpansionUsageAsync(result.Snippet!.Id);

        var persisted = await repository.GetByIdAsync(result.Snippet.Id);
        Assert.NotNull(persisted);
        Assert.Equal(1, persisted.UsageCount);
        Assert.NotNull(persisted.LastUsedAt);
    }

    [Fact]
    public async Task ProcessKeyAsync_WithSqliteRepository_IgnoresTrashAndInactive()
    {
        var repository = new SnippetRepository(ConnectionFactory);
        var variableService = new VariableResolutionService();
        using var engine = new SnippetExpansionEngine(repository, variableService);

        var trashSnippet = Snippet.Create("basura", "Papelera", "Texto papelera");
        trashSnippet.MoveToTrash();
        await repository.AddAsync(trashSnippet);

        foreach (var c in "basura")
        {
            await engine.ProcessKeyAsync(c);
        }
        var trashResult = await engine.ProcessKeyAsync(' ');
        Assert.Equal(ExpansionStatus.NoMatch, trashResult.Status);

        var inactiveSnippet = Snippet.Create("inactivo", "Inactivo", "Texto inactivo");
        inactiveSnippet.ToggleActive();
        await repository.AddAsync(inactiveSnippet);

        foreach (var c in "inactivo")
        {
            await engine.ProcessKeyAsync(c);
        }
        var inactiveResult = await engine.ProcessKeyAsync(' ');
        Assert.Equal(ExpansionStatus.NoMatch, inactiveResult.Status);
    }
}
