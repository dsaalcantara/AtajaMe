using AtajaMe.Application.DTOs;
using AtajaMe.Application.Exceptions;
using AtajaMe.Application.Folders;
using AtajaMe.Infrastructure.Repositories;
using AtajaMe.Tests.Infrastructure;

namespace AtajaMe.Tests.Application;

public sealed class FolderUseCaseTests : SqliteTestBase
{
    private readonly FolderRepository _folderRepository;
    private readonly CreateFolderUseCase _createUseCase;
    private readonly UpdateFolderUseCase _updateUseCase;
    private readonly DeleteFolderUseCase _deleteUseCase;
    private readonly GetFoldersUseCase _getUseCase;

    public FolderUseCaseTests()
    {
        _folderRepository = new FolderRepository(ConnectionFactory);
        _createUseCase = new CreateFolderUseCase(_folderRepository);
        _updateUseCase = new UpdateFolderUseCase(_folderRepository);
        _deleteUseCase = new DeleteFolderUseCase(_folderRepository);
        _getUseCase = new GetFoldersUseCase(_folderRepository);
    }

    [Fact]
    public async Task CreateFolder_UpTo3Levels_Succeeds()
    {
        var level1 = await _createUseCase.ExecuteAsync(new CreateFolderCommand("Nivel 1"));
        var level2 = await _createUseCase.ExecuteAsync(new CreateFolderCommand("Nivel 2", level1.Id));
        var level3 = await _createUseCase.ExecuteAsync(new CreateFolderCommand("Nivel 3", level2.Id));

        Assert.Equal(1, await _folderRepository.GetDepthAsync(level1.Id));
        Assert.Equal(2, await _folderRepository.GetDepthAsync(level2.Id));
        Assert.Equal(3, await _folderRepository.GetDepthAsync(level3.Id));
    }

    [Fact]
    public async Task CreateFolder_AtLevel4_ThrowsMaxFolderDepthExceededException()
    {
        var level1 = await _createUseCase.ExecuteAsync(new CreateFolderCommand("Nivel 1"));
        var level2 = await _createUseCase.ExecuteAsync(new CreateFolderCommand("Nivel 2", level1.Id));
        var level3 = await _createUseCase.ExecuteAsync(new CreateFolderCommand("Nivel 3", level2.Id));

        var commandLevel4 = new CreateFolderCommand("Nivel 4 Prohibido", level3.Id);

        await Assert.ThrowsAsync<MaxFolderDepthExceededException>(() => _createUseCase.ExecuteAsync(commandLevel4));
    }

    [Fact]
    public async Task UpdateFolder_CannotSetSelfAsParent()
    {
        var folder = await _createUseCase.ExecuteAsync(new CreateFolderCommand("Carpeta"));

        var command = new UpdateFolderCommand(folder.Id, "Carpeta", folder.Id);
        await Assert.ThrowsAsync<FolderCycleException>(() => _updateUseCase.ExecuteAsync(command));
    }

    [Fact]
    public async Task UpdateFolder_CannotMoveToOwnChild()
    {
        var parent = await _createUseCase.ExecuteAsync(new CreateFolderCommand("Padre"));
        var child = await _createUseCase.ExecuteAsync(new CreateFolderCommand("Hijo", parent.Id));

        var command = new UpdateFolderCommand(parent.Id, "Padre", child.Id);
        await Assert.ThrowsAsync<FolderCycleException>(() => _updateUseCase.ExecuteAsync(command));
    }

    [Fact]
    public async Task UpdateFolder_CannotExceedMaxDepthWhenMovingSubtree()
    {
        // Rama A: A1 -> A2
        var a1 = await _createUseCase.ExecuteAsync(new CreateFolderCommand("A1"));
        var a2 = await _createUseCase.ExecuteAsync(new CreateFolderCommand("A2", a1.Id));

        // Rama B: B1 -> B2
        var b1 = await _createUseCase.ExecuteAsync(new CreateFolderCommand("B1"));
        var b2 = await _createUseCase.ExecuteAsync(new CreateFolderCommand("B2", b1.Id));

        // Si intentamos mover A1 como hijo de B2, la profundidad total sería B1 (1) -> B2 (2) -> A1 (3) -> A2 (4) -> Debe lanzar excepción
        var command = new UpdateFolderCommand(a1.Id, "A1", b2.Id);
        await Assert.ThrowsAsync<MaxFolderDepthExceededException>(() => _updateUseCase.ExecuteAsync(command));
    }

    [Fact]
    public async Task DeleteFolder_RemovesFolder()
    {
        var folder = await _createUseCase.ExecuteAsync(new CreateFolderCommand("Eliminar"));

        await _deleteUseCase.ExecuteAsync(folder.Id);

        var retrieved = await _getUseCase.GetByIdAsync(folder.Id);
        Assert.Null(retrieved);
    }
}
