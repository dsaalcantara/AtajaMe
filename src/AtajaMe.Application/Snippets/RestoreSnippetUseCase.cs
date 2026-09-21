using AtajaMe.Application.DTOs;
using AtajaMe.Application.Exceptions;
using AtajaMe.Domain.Repositories;

namespace AtajaMe.Application.Snippets;

/// <summary>
/// Caso de uso para restaurar un atajo desde la papelera, validando que su código no colisione con otro activo.
/// </summary>
public sealed class RestoreSnippetUseCase
{
    private readonly ISnippetRepository _snippetRepository;

    public RestoreSnippetUseCase(ISnippetRepository snippetRepository)
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

        if (snippet.IsInTrash)
        {
            // Validar que el código no colisione con otro atajo no eliminado
            var codeExists = await _snippetRepository.ExistsCodeAsync(snippet.Code, snippet.Id, cancellationToken);
            if (codeExists)
            {
                throw new DuplicateSnippetCodeException(snippet.Code);
            }

            snippet.RestoreFromTrash();
            await _snippetRepository.UpdateAsync(snippet, cancellationToken);
        }

        return SnippetDto.FromDomain(snippet);
    }
}
