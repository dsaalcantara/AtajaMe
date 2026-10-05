using AtajaMe.Application.DTOs;
using AtajaMe.Application.Exceptions;
using AtajaMe.Application.Folders;
using AtajaMe.Application.Snippets;
using AtajaMe.Domain.Entities;
using AtajaMe.Infrastructure.Repositories;
using AtajaMe.Tests.Infrastructure;

namespace AtajaMe.Tests.Application;

public sealed class TrashAndPurgeUseCaseTests : SqliteTestBase
{
    private readonly SnippetRepository _snippetRepository;
    private readonly DeleteSnippetToTrashUseCase _trashUseCase;
    private readonly RestoreSnippetUseCase _restoreUseCase;
    private readonly PurgeTrashUseCase _purgeUseCase;

    public TrashAndPurgeUseCaseTests()
    {
        _snippetRepository = new SnippetRepository(ConnectionFactory);
        _trashUseCase = new DeleteSnippetToTrashUseCase(_snippetRepository);
        _restoreUseCase = new RestoreSnippetUseCase(_snippetRepository);
        _purgeUseCase = new PurgeTrashUseCase(_snippetRepository);
    }

    [Fact]
    public async Task DeleteSnippetToTrash_MovesSnippetToTrash()
    {
        var snippet = Snippet.Create("borrame", "A eliminar", "Texto");
        await _snippetRepository.AddAsync(snippet);

        var result = await _trashUseCase.ExecuteAsync(snippet.Id);

        Assert.True(result.IsInTrash);
        Assert.NotNull(result.DeletedAt);

        var trashList = await _snippetRepository.GetTrashAsync();
        Assert.Single(trashList);
        Assert.Equal(snippet.Id, trashList[0].Id);
    }

    [Fact]
    public async Task RestoreSnippet_RestoresSnippetFromTrash()
    {
        var snippet = Snippet.Create("recuperame", "A recuperar", "Texto");
        await _snippetRepository.AddAsync(snippet);
        await _trashUseCase.ExecuteAsync(snippet.Id);

        var restored = await _restoreUseCase.ExecuteAsync(snippet.Id);

        Assert.False(restored.IsInTrash);
        Assert.Null(restored.DeletedAt);

        var activeList = await _snippetRepository.GetAllActiveAsync();
        Assert.Single(activeList);
        Assert.Equal(snippet.Id, activeList[0].Id);
    }

    [Fact]
    public async Task CreateSnippet_WhenCodeIsUsedByTrashedSnippet_ThrowsDuplicateSnippetCodeException()
    {
        var folderRepo = new FolderRepository(ConnectionFactory);
        var createUseCase = new CreateSnippetUseCase(_snippetRepository, folderRepo);

        var snippet1 = Snippet.Create("codigo_reservado", "Primero", "Texto 1");
        await _snippetRepository.AddAsync(snippet1);
        await _trashUseCase.ExecuteAsync(snippet1.Id);

        // Al intentar crear un atajo con el código de un atajo que está en la papelera, debe rechazarlo indicando que está en papelera
        var command = new CreateSnippetCommand("codigo_reservado", "Segundo", "Texto 2");
        await Assert.ThrowsAsync<SnippetInTrashException>(() => createUseCase.ExecuteAsync(command));
    }

    [Fact]
    public async Task RestoreSnippet_WhenSnippetNotFound_ThrowsSnippetNotFoundException()
    {
        await Assert.ThrowsAsync<SnippetNotFoundException>(() => _restoreUseCase.ExecuteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task PurgeAllAsync_RemovesAllTrashedSnippetsPermanently()
    {
        var s1 = Snippet.Create("t1", "Papelera 1", "Texto 1");
        var s2 = Snippet.Create("t2", "Papelera 2", "Texto 2");
        var active = Snippet.Create("activo", "Activo", "Texto");

        await _snippetRepository.AddAsync(s1);
        await _snippetRepository.AddAsync(s2);
        await _snippetRepository.AddAsync(active);

        await _trashUseCase.ExecuteAsync(s1.Id);
        await _trashUseCase.ExecuteAsync(s2.Id);

        await _purgeUseCase.PurgeAllAsync();

        var trash = await _snippetRepository.GetTrashAsync();
        Assert.Empty(trash);

        var activeSnippets = await _snippetRepository.GetAllActiveAsync();
        Assert.Single(activeSnippets);
        Assert.Equal(active.Id, activeSnippets[0].Id);
    }
}
