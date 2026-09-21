using AtajaMe.Application.Exceptions;
using AtajaMe.Application.Snippets;
using AtajaMe.Domain.Entities;
using AtajaMe.Infrastructure.Repositories;
using AtajaMe.Tests.Infrastructure;

namespace AtajaMe.Tests.Application;

public sealed class DuplicateSnippetUseCaseTests : SqliteTestBase
{
    private readonly SnippetRepository _snippetRepository;
    private readonly DuplicateSnippetUseCase _useCase;

    public DuplicateSnippetUseCaseTests()
    {
        _snippetRepository = new SnippetRepository(ConnectionFactory);
        _useCase = new DuplicateSnippetUseCase(_snippetRepository);
    }

    [Fact]
    public async Task ExecuteAsync_WithExplicitNewCode_DuplicatesSnippet()
    {
        var snippet = Snippet.Create("orig", "Original", "Texto Original");
        await _snippetRepository.AddAsync(snippet);

        var copy = await _useCase.ExecuteAsync(snippet.Id, "orig_v2");

        Assert.NotEqual(snippet.Id, copy.Id);
        Assert.Equal("orig_v2", copy.Code);
        Assert.Equal("Original (Copia)", copy.Name);
        Assert.Equal("Texto Original", copy.Content);

        var copyInDb = await _snippetRepository.GetByIdAsync(copy.Id);
        Assert.NotNull(copyInDb);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutExplicitCode_GeneratesUniqueSuffix()
    {
        var snippet = Snippet.Create("plantilla", "Plantilla", "Texto");
        await _snippetRepository.AddAsync(snippet);

        var copy1 = await _useCase.ExecuteAsync(snippet.Id);
        Assert.Equal("plantilla_copia", copy1.Code);

        var copy2 = await _useCase.ExecuteAsync(snippet.Id);
        Assert.Equal("plantilla_copia2", copy2.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTargetCodeCollides_ThrowsDuplicateSnippetCodeException()
    {
        var s1 = Snippet.Create("orig", "Original", "Texto");
        var s2 = Snippet.Create("ocupado", "Ocupado", "Texto");
        await _snippetRepository.AddAsync(s1);
        await _snippetRepository.AddAsync(s2);

        await Assert.ThrowsAsync<DuplicateSnippetCodeException>(() => _useCase.ExecuteAsync(s1.Id, "ocupado"));
    }
}
