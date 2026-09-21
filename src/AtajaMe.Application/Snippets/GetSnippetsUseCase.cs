using AtajaMe.Application.DTOs;
using AtajaMe.Domain.Repositories;

namespace AtajaMe.Application.Snippets;

/// <summary>
/// Caso de uso para consultas y búsquedas de atajos de texto.
/// </summary>
public sealed class GetSnippetsUseCase
{
    private readonly ISnippetRepository _snippetRepository;

    public GetSnippetsUseCase(ISnippetRepository snippetRepository)
    {
        _snippetRepository = snippetRepository;
    }

    public async Task<SnippetDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var snippet = await _snippetRepository.GetByIdAsync(id, cancellationToken);
        return snippet != null ? SnippetDto.FromDomain(snippet) : null;
    }

    public async Task<IReadOnlyList<SnippetDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var snippets = await _snippetRepository.GetAllActiveAsync(cancellationToken);
        return snippets.Select(SnippetDto.FromDomain).ToList();
    }

    public async Task<IReadOnlyList<SnippetDto>> GetByFolderIdAsync(Guid? folderId, CancellationToken cancellationToken = default)
    {
        var snippets = await _snippetRepository.GetByFolderIdAsync(folderId, cancellationToken);
        return snippets.Select(SnippetDto.FromDomain).ToList();
    }

    public async Task<IReadOnlyList<SnippetDto>> GetFavoritesAsync(CancellationToken cancellationToken = default)
    {
        var snippets = await _snippetRepository.GetFavoritesAsync(cancellationToken);
        return snippets.Select(SnippetDto.FromDomain).ToList();
    }

    public async Task<IReadOnlyList<SnippetDto>> GetRecentAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var snippets = await _snippetRepository.GetRecentAsync(limit, cancellationToken);
        return snippets.Select(SnippetDto.FromDomain).ToList();
    }

    public async Task<IReadOnlyList<SnippetDto>> GetTrashAsync(CancellationToken cancellationToken = default)
    {
        var snippets = await _snippetRepository.GetTrashAsync(cancellationToken);
        return snippets.Select(SnippetDto.FromDomain).ToList();
    }

    public async Task<IReadOnlyList<SnippetDto>> SearchAsync(string query, Guid? folderId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetByFolderIdAsync(folderId, cancellationToken);
        }

        var snippets = await _snippetRepository.SearchAsync(query, folderId, cancellationToken);
        return snippets.Select(SnippetDto.FromDomain).ToList();
    }
}
