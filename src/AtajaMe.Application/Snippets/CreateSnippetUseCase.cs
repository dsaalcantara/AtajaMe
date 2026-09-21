using AtajaMe.Application.DTOs;
using AtajaMe.Application.Exceptions;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Repositories;

namespace AtajaMe.Application.Snippets;

/// <summary>
/// Caso de uso para la creación de un nuevo atajo de texto.
/// </summary>
public sealed class CreateSnippetUseCase
{
    private readonly ISnippetRepository _snippetRepository;
    private readonly IFolderRepository _folderRepository;

    public CreateSnippetUseCase(ISnippetRepository snippetRepository, IFolderRepository folderRepository)
    {
        _snippetRepository = snippetRepository;
        _folderRepository = folderRepository;
    }

    public async Task<SnippetDto> ExecuteAsync(CreateSnippetCommand command, CancellationToken cancellationToken = default)
    {
        if (command.FolderId.HasValue)
        {
            var folder = await _folderRepository.GetByIdAsync(command.FolderId.Value, cancellationToken);
            if (folder is null)
            {
                throw new FolderNotFoundException(command.FolderId.Value);
            }
        }

        if (await _snippetRepository.ExistsCodeAsync(command.Code, null, cancellationToken))
        {
            throw new DuplicateSnippetCodeException(command.Code);
        }

        var snippet = Snippet.Create(
            command.Code,
            command.Name,
            command.Content,
            command.FolderId,
            command.CaseSensitive);

        await _snippetRepository.AddAsync(snippet, cancellationToken);
        return SnippetDto.FromDomain(snippet);
    }
}
