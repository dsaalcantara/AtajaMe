using AtajaMe.Application.DTOs;
using AtajaMe.Application.Exceptions;
using AtajaMe.Domain.Repositories;

namespace AtajaMe.Application.Snippets;

/// <summary>
/// Caso de uso para trasladar un atajo a la papelera.
/// </summary>
public sealed class DeleteSnippetToTrashUseCase
{
    private readonly ISnippetRepository _snippetRepository;

    public DeleteSnippetToTrashUseCase(ISnippetRepository snippetRepository)
    {
        _snippetRepository = snippetRepository;
    }

    public async Task<SnippetDto> ExecuteAsync(Guid snippetId, CancellationToken cancellationToken = default)
    {
        var snippet = await _snippetRepository.GetByIdAsync(snippetId, cancellationToken);
        if (snippet is null)
        {
            throw new SnippetNotFoundException(snippetId);
        }

        if (!snippet.IsInTrash)
        {
            snippet.MoveToTrash();
            await _snippetRepository.UpdateAsync(snippet, cancellationToken);
        }

        return SnippetDto.FromDomain(snippet);
    }
}
