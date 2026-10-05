using AtajaMe.Application.DTOs;
using AtajaMe.Application.Exceptions;
using AtajaMe.Domain.Repositories;

namespace AtajaMe.Application.Snippets;

/// <summary>
/// Caso de uso para la edición de un atajo de texto existente.
/// </summary>
public sealed class UpdateSnippetUseCase
{
    private readonly ISnippetRepository _snippetRepository;
    private readonly IFolderRepository _folderRepository;

    public UpdateSnippetUseCase(ISnippetRepository snippetRepository, IFolderRepository folderRepository)
    {
        _snippetRepository = snippetRepository;
        _folderRepository = folderRepository;
    }

    public async Task<SnippetDto> ExecuteAsync(UpdateSnippetCommand command, CancellationToken cancellationToken = default)
    {
        var snippet = await _snippetRepository.GetByIdAsync(command.Id, cancellationToken);
        if (snippet is null)
        {
            throw new SnippetNotFoundException(command.Id);
        }

        if (command.FolderId.HasValue)
        {
            var folder = await _folderRepository.GetByIdAsync(command.FolderId.Value, cancellationToken);
            if (folder is null)
            {
                throw new FolderNotFoundException(command.FolderId.Value);
            }
        }

        if (await _snippetRepository.ExistsCodeAsync(command.Code, command.Id, cancellationToken))
        {
            var existing = await _snippetRepository.GetAnyByCodeAsync(command.Code, cancellationToken);
            if (existing != null && existing.IsInTrash)
            {
                throw new SnippetInTrashException(command.Code, existing.Id);
            }

            throw new DuplicateSnippetCodeException(command.Code);
        }

        snippet.Update(
            command.Code,
            command.Name,
            command.Content,
            command.FolderId,
            command.CaseSensitive);

        await _snippetRepository.UpdateAsync(snippet, cancellationToken);
        return SnippetDto.FromDomain(snippet);
    }
}
