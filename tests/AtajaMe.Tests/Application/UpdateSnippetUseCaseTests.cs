using AtajaMe.Application.DTOs;
using AtajaMe.Application.Exceptions;
using AtajaMe.Application.Snippets;
using AtajaMe.Domain.Entities;
using AtajaMe.Infrastructure.Repositories;
using AtajaMe.Tests.Infrastructure;

namespace AtajaMe.Tests.Application;

public sealed class UpdateSnippetUseCaseTests : SqliteTestBase
{
    private readonly SnippetRepository _snippetRepository;
    private readonly FolderRepository _folderRepository;
    private readonly UpdateSnippetUseCase _useCase;

    public UpdateSnippetUseCaseTests()
    {
        _snippetRepository = new SnippetRepository(ConnectionFactory);
        _folderRepository = new FolderRepository(ConnectionFactory);
        _useCase = new UpdateSnippetUseCase(_snippetRepository, _folderRepository);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesAttributesSuccessfully()
    {
        var snippet = Snippet.Create("inicial", "Nombre Inicial", "Texto Inicial");
        await _snippetRepository.AddAsync(snippet);

        var command = new UpdateSnippetCommand(snippet.Id, "modificado", "Nombre Modificado", "Texto Modificado", null, true);
        var result = await _useCase.ExecuteAsync(command);

        Assert.Equal("modificado", result.Code);
        Assert.Equal("Nombre Modificado", result.Name);
        Assert.Equal("Texto Modificado", result.Content);
        Assert.True(result.CaseSensitive);

        var inDb = await _snippetRepository.GetByIdAsync(snippet.Id);
        Assert.NotNull(inDb);
        Assert.Equal("modificado", inDb.Code);
    }

    [Fact]
    public async Task ExecuteAsync_RetainsSameCodeWithoutConflict()
    {
        var snippet = Snippet.Create("codigo_fijo", "Nombre 1", "Texto");
        await _snippetRepository.AddAsync(snippet);

        var command = new UpdateSnippetCommand(snippet.Id, "codigo_fijo", "Nombre 2", "Nuevo texto");
        var result = await _useCase.ExecuteAsync(command);

        Assert.Equal("codigo_fijo", result.Code);
        Assert.Equal("Nombre 2", result.Name);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTargetCodeMatchesAnotherSnippet_ThrowsDuplicateSnippetCodeException()
    {
        var s1 = Snippet.Create("codigo_a", "A", "Texto A");
        var s2 = Snippet.Create("codigo_b", "B", "Texto B");
        await _snippetRepository.AddAsync(s1);
        await _snippetRepository.AddAsync(s2);

        var command = new UpdateSnippetCommand(s2.Id, "codigo_a", "B Renombrado", "Texto");
        await Assert.ThrowsAsync<DuplicateSnippetCodeException>(() => _useCase.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_WhenSnippetDoesNotExist_ThrowsSnippetNotFoundException()
    {
        var command = new UpdateSnippetCommand(Guid.NewGuid(), "codigo", "Nombre", "Texto");
        await Assert.ThrowsAsync<SnippetNotFoundException>(() => _useCase.ExecuteAsync(command));
    }
}
