using AtajaMe.Application.DTOs;
using AtajaMe.Domain.Repositories;

namespace AtajaMe.Application.Folders;

/// <summary>
/// Caso de uso para obtener las carpetas del sistema.
/// </summary>
public sealed class GetFoldersUseCase
{
    private readonly IFolderRepository _folderRepository;

    public GetFoldersUseCase(IFolderRepository folderRepository)
    {
        _folderRepository = folderRepository;
    }

    public async Task<IReadOnlyList<FolderDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var folders = await _folderRepository.GetAllAsync(cancellationToken);
        return folders.Select(FolderDto.FromDomain).ToList();
    }

    public async Task<FolderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var folder = await _folderRepository.GetByIdAsync(id, cancellationToken);
        return folder != null ? FolderDto.FromDomain(folder) : null;
    }
}
