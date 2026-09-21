using AtajaMe.Application.DTOs;
using AtajaMe.Application.Exceptions;
using AtajaMe.Domain.Repositories;

namespace AtajaMe.Application.Snippets;

/// <summary>
/// Casos de uso para alternar estados rápidos de un atajo (favorito / activo).
/// </summary>
public sealed class ToggleSnippetStateUseCase
{
    private readonly ISnippetRepository _snippetRepository;

    public ToggleSnippetStateUseCase(ISnippetRepository snippetRepository)
    {
        _snippetRepository = snippetRepository;
    }

    public async Task<SnippetDto> ToggleFavoriteAsync(Guid snippetId, CancellationToken cancellationToken = default)
    {
        var snippet = await _snippetRepository.GetByIdAsync(snippetId, cancellationToken);
        if (snippet is null)
        {
            throw new SnippetNotFoundException(snippetId);
        }

        snippet.ToggleFavorite();
        await _snippetRepository.UpdateAsync(snippet, cancellationToken);
        return SnippetDto.FromDomain(snippet);
    }

    public async Task<SnippetDto> ToggleActiveAsync(Guid snippetId, CancellationToken cancellationToken = default)
    {
        var snippet = await _snippetRepository.GetByIdAsync(snippetId, cancellationToken);
        if (snippet is null)
        {
            throw new SnippetNotFoundException(snippetId);
        }

        snippet.ToggleActive();
        await _snippetRepository.UpdateAsync(snippet, cancellationToken);
        return SnippetDto.FromDomain(snippet);
    }
}
