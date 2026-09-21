using AtajaMe.Domain.Entities;
using AtajaMe.Infrastructure.Repositories;

namespace AtajaMe.Tests.Infrastructure;

public sealed class FolderRepositoryTests : SqliteTestBase
{
    private readonly FolderRepository _repository;

    public FolderRepositoryTests()
    {
        _repository = new FolderRepository(ConnectionFactory);
    }

    [Fact]
    public async Task AddAndGetByIdAsync_PersistsFolderSuccessfully()
    {
        var folder = Folder.Create("Trabajo", null, 1);
        await _repository.AddAsync(folder);

        var retrieved = await _repository.GetByIdAsync(folder.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(folder.Id, retrieved.Id);
        Assert.Equal("Trabajo", retrieved.Name);
        Assert.Null(retrieved.ParentId);
        Assert.Equal(1, retrieved.SortOrder);
    }

    [Fact]
    public async Task GetDepthAsync_CalculatesHierarchicalDepthAccurately()
    {
        var root = Folder.Create("Nivel 1");
        await _repository.AddAsync(root);

        var child = Folder.Create("Nivel 2", root.Id);
        await _repository.AddAsync(child);

        var grandchild = Folder.Create("Nivel 3", child.Id);
        await _repository.AddAsync(grandchild);

        var rootDepth = await _repository.GetDepthAsync(root.Id);
        var childDepth = await _repository.GetDepthAsync(child.Id);
        var grandchildDepth = await _repository.GetDepthAsync(grandchild.Id);

        Assert.Equal(1, rootDepth);
        Assert.Equal(2, childDepth);
        Assert.Equal(3, grandchildDepth);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesNameAndParent()
    {
        var f1 = Folder.Create("Original");
        await _repository.AddAsync(f1);

        var f2 = Folder.Create("Nuevo Padre");
        await _repository.AddAsync(f2);

        f1.UpdateName("Renombrada");
        f1.MoveTo(f2.Id);
        f1.SetSortOrder(5);
        await _repository.UpdateAsync(f1);

        var updated = await _repository.GetByIdAsync(f1.Id);
        Assert.NotNull(updated);
        Assert.Equal("Renombrada", updated.Name);
        Assert.Equal(f2.Id, updated.ParentId);
        Assert.Equal(5, updated.SortOrder);
    }

    [Fact]
    public async Task DeleteAsync_DeletesFolderAndCascadesSubfolders()
    {
        var parent = Folder.Create("Padre");
        await _repository.AddAsync(parent);

        var child = Folder.Create("Hijo", parent.Id);
        await _repository.AddAsync(child);

        await _repository.DeleteAsync(parent.Id);

        Assert.Null(await _repository.GetByIdAsync(parent.Id));
        Assert.Null(await _repository.GetByIdAsync(child.Id));
    }
}
