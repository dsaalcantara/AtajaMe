using AtajaMe.Application.DTOs;
using AtajaMe.Application.Exceptions;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Repositories;

namespace AtajaMe.Application.Folders;

/// <summary>
/// Caso de uso para crear una nueva carpeta respetando el límite de 3 niveles jerárquicos.
/// </summary>
public sealed class CreateFolderUseCase
{
    private readonly IFolderRepository _folderRepository;

    public CreateFolderUseCase(IFolderRepository folderRepository)
    {
        _folderRepository = folderRepository;
    }

    public async Task<FolderDto> ExecuteAsync(CreateFolderCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ParentId.HasValue)
        {
            var parent = await _folderRepository.GetByIdAsync(command.ParentId.Value, cancellationToken);
            if (parent is null)
            {
                throw new FolderNotFoundException(command.ParentId.Value);
            }

            var parentDepth = await _folderRepository.GetDepthAsync(command.ParentId.Value, cancellationToken);
            if (parentDepth >= Folder.MaxHierarchyDepth)
            {
                throw new MaxFolderDepthExceededException(Folder.MaxHierarchyDepth);
            }
        }

        var folder = Folder.Create(command.Name, command.ParentId, command.SortOrder);
        await _folderRepository.AddAsync(folder, cancellationToken);
        return FolderDto.FromDomain(folder);
    }
}
