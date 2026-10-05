using AtajaMe.Application.DTOs;
using AtajaMe.Application.Snippets;
using AtajaMe.Infrastructure.Repositories;
using AtajaMe.Tests.Infrastructure;

namespace AtajaMe.Tests.Application;

public sealed class SearchAndFilterSnippetsUseCaseTests : SqliteTestBase
{
    private readonly SnippetRepository _snippetRepository;
    private readonly FolderRepository _folderRepository;
    private readonly CreateSnippetUseCase _createSnippetUseCase;
    private readonly GetSnippetsUseCase _getSnippetsUseCase;
    private readonly ToggleSnippetStateUseCase _toggleUseCase;

    public SearchAndFilterSnippetsUseCaseTests()
    {
        _snippetRepository = new SnippetRepository(ConnectionFactory);
        _folderRepository = new FolderRepository(ConnectionFactory);
        _createSnippetUseCase = new CreateSnippetUseCase(_snippetRepository, _folderRepository);
        _getSnippetsUseCase = new GetSnippetsUseCase(_snippetRepository);
        _toggleUseCase = new ToggleSnippetStateUseCase(_snippetRepository);
    }

    [Fact]
    public async Task SearchAsync_FindsMatchingSnippetsAcrossFields()
    {
        await _createSnippetUseCase.ExecuteAsync(new CreateSnippetCommand("cli_ls", "Listar Directorio", "Get-ChildItem -Force"));
        await _createSnippetUseCase.ExecuteAsync(new CreateSnippetCommand("cli_cd", "Cambiar Directorio", "Set-Location"));
        await _createSnippetUseCase.ExecuteAsync(new CreateSnippetCommand("git_st", "Estado Git", "git status"));

        var resultByCode = await _getSnippetsUseCase.SearchAsync("cli_");
        Assert.Equal(2, resultByCode.Count);

        var resultByName = await _getSnippetsUseCase.SearchAsync("Directorio");
        Assert.Equal(2, resultByName.Count);

        var resultContent = await _getSnippetsUseCase.SearchAsync("Set-Location");
        Assert.Single(resultContent);
        Assert.Equal("cli_cd", resultContent[0].Code);
    }

    [Fact]
    public async Task ToggleFavoriteAndActive_UpdatesCorrectlyAndAffectsQueries()
    {
        var snippet = await _createSnippetUseCase.ExecuteAsync(new CreateSnippetCommand("fav", "Favorito", "Contenido"));

        Assert.False(snippet.IsFavorite);
        Assert.True(snippet.IsActive);

        // Conmutar a favorito
        var favResult = await _toggleUseCase.ToggleFavoriteAsync(snippet.Id);
        Assert.True(favResult.IsFavorite);

        var favorites = await _getSnippetsUseCase.GetFavoritesAsync();
        Assert.Single(favorites);
        Assert.Equal(snippet.Id, favorites[0].Id);

        // Desactivar
        var deactResult = await _toggleUseCase.ToggleActiveAsync(snippet.Id);
        Assert.False(deactResult.IsActive);

        var activeSnippets = await _getSnippetsUseCase.GetAllActiveAsync();
        Assert.Empty(activeSnippets);

        var allLibrarySnippets = await _getSnippetsUseCase.GetAllAsync();
        Assert.Single(allLibrarySnippets);
        Assert.Equal(snippet.Id, allLibrarySnippets[0].Id);
        Assert.False(allLibrarySnippets[0].IsActive);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsUsedSnippetsOrderedByLastUsed()
    {
        var s1 = await _createSnippetUseCase.ExecuteAsync(new CreateSnippetCommand("rec1", "Reciente 1", "Texto"));
        var s2 = await _createSnippetUseCase.ExecuteAsync(new CreateSnippetCommand("rec2", "Reciente 2", "Texto"));

        var domain1 = await _snippetRepository.GetByIdAsync(s1.Id);
        var domain2 = await _snippetRepository.GetByIdAsync(s2.Id);

        domain1!.RecordUsage();
        await _snippetRepository.UpdateAsync(domain1);

        await Task.Delay(10); // Garantizar diferencia temporal

        domain2!.RecordUsage();
        await _snippetRepository.UpdateAsync(domain2);

        var recent = await _getSnippetsUseCase.GetRecentAsync(5);
        Assert.Equal(2, recent.Count);
        Assert.Equal(s2.Id, recent[0].Id); // El más reciente primero
        Assert.Equal(s1.Id, recent[1].Id);
    }
}
