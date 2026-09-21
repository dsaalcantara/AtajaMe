using AtajaMe.Application.DTOs;
using AtajaMe.Application.Exceptions;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Repositories;

namespace AtajaMe.Application.Folders;

/// <summary>
/// Caso de uso para editar, mover o reordenar una carpeta con control de ciclos y profundidad.
/// </summary>
public sealed class UpdateFolderUseCase
{
    private readonly IFolderRepository _folderRepository;

    public UpdateFolderUseCase(IFolderRepository folderRepository)
    {
        _folderRepository = folderRepository;
    }

    public async Task<FolderDto> ExecuteAsync(UpdateFolderCommand command, CancellationToken cancellationToken = default)
    {
        var folder = await _folderRepository.GetByIdAsync(command.Id, cancellationToken);
        if (folder is null)
        {
            throw new FolderNotFoundException(command.Id);
        }

        if (command.ParentId.HasValue)
        {
            if (command.ParentId.Value == folder.Id)
            {
                throw new FolderCycleException("Una carpeta no puede ser su propio contenedor.");
            }

            var parent = await _folderRepository.GetByIdAsync(command.ParentId.Value, cancellationToken);
            if (parent is null)
            {
                throw new FolderNotFoundException(command.ParentId.Value);
            }

            // Verificar que no se cree un ciclo (que el nuevo padre no sea un descendiente de esta carpeta)
            var allFolders = await _folderRepository.GetAllAsync(cancellationToken);
            var folderMap = allFolders.ToDictionary(f => f.Id);

            var current = parent;
            while (current.ParentId.HasValue)
            {
                if (current.ParentId.Value == folder.Id)
                {
                    throw new FolderCycleException("No se puede mover una carpeta dentro de uno de sus propios descendientes.");
                }

                if (!folderMap.TryGetValue(current.ParentId.Value, out current!))
                {
                    break;
                }
            }

            // Calcular la profundidad que tendría la rama resultante
            var parentDepth = await _folderRepository.GetDepthAsync(command.ParentId.Value, cancellationToken);
            var subtreeDepth = CalculateSubtreeDepth(folder.Id, allFolders);

            if (parentDepth + subtreeDepth > Folder.MaxHierarchyDepth)
            {
                throw new MaxFolderDepthExceededException(Folder.MaxHierarchyDepth);
            }
        }

        folder.UpdateName(command.Name);
        folder.MoveTo(command.ParentId);
        folder.SetSortOrder(command.SortOrder);

        await _folderRepository.UpdateAsync(folder, cancellationToken);
        return FolderDto.FromDomain(folder);
    }

    private static int CalculateSubtreeDepth(Guid folderId, IReadOnlyList<Folder> allFolders)
    {
        var children = allFolders.Where(f => f.ParentId == folderId).ToList();
        if (children.Count == 0)
        {
            return 1;
        }

        return 1 + children.Max(c => CalculateSubtreeDepth(c.Id, allFolders));
    }
}
