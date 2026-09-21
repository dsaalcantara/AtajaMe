using AtajaMe.Application.Exceptions;
using AtajaMe.Domain.Repositories;

namespace AtajaMe.Application.Folders;

/// <summary>
/// Caso de uso para eliminar una carpeta.
/// </summary>
public sealed class DeleteFolderUseCase
{
    private readonly IFolderRepository _folderRepository;

    public DeleteFolderUseCase(IFolderRepository folderRepository)
    {
        _folderRepository = folderRepository;
    }

    public async Task ExecuteAsync(Guid folderId, CancellationToken cancellationToken = default)
    {
        var folder = await _folderRepository.GetByIdAsync(folderId, cancellationToken);
        if (folder is null)
        {
            throw new FolderNotFoundException(folderId);
        }

        await _folderRepository.DeleteAsync(folderId, cancellationToken);
    }
}
