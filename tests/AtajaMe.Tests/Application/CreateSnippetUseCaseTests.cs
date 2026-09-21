using AtajaMe.Application.DTOs;
using AtajaMe.Application.Exceptions;
using AtajaMe.Application.Snippets;
using AtajaMe.Domain.Entities;
using AtajaMe.Infrastructure.Repositories;
using AtajaMe.Tests.Infrastructure;

namespace AtajaMe.Tests.Application;

public sealed class CreateSnippetUseCaseTests : SqliteTestBase
{
    private readonly SnippetRepository _snippetRepository;
    private readonly FolderRepository _folderRepository;
    private readonly CreateSnippetUseCase _useCase;

    public CreateSnippetUseCaseTests()
    {
        _snippetRepository = new SnippetRepository(ConnectionFactory);
        _folderRepository = new FolderRepository(ConnectionFactory);
        _useCase = new CreateSnippetUseCase(_snippetRepository, _folderRepository);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidData_CreatesAndReturnsSnippet()
    {
        var command = new CreateSnippetCommand("test", "Test Snippet", "Contenido de prueba");

        var result = await _useCase.ExecuteAsync(command);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("test", result.Code);
        Assert.Equal("Test Snippet", result.Name);
        Assert.Equal("Contenido de prueba", result.Content);
        Assert.True(result.IsActive);
        Assert.False(result.IsInTrash);

        var fromDb = await _snippetRepository.GetByIdAsync(result.Id);
        Assert.NotNull(fromDb);
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingCode_ThrowsDuplicateSnippetCodeException()
    {
        var existing = Snippet.Create("repetido", "Existente", "Texto");
        await _snippetRepository.AddAsync(existing);

        var command = new CreateSnippetCommand("repetido", "Nuevo con mismo código", "Texto");

        await Assert.ThrowsAsync<DuplicateSnippetCodeException>(() => _useCase.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentFolder_ThrowsFolderNotFoundException()
    {
        var command = new CreateSnippetCommand("codigo", "Nombre", "Texto", Guid.NewGuid());

        await Assert.ThrowsAsync<FolderNotFoundException>(() => _useCase.ExecuteAsync(command));
    }
}
