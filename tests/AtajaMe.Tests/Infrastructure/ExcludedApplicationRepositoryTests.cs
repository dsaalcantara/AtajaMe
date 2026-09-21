using AtajaMe.Domain.Entities;
using AtajaMe.Infrastructure.Repositories;

namespace AtajaMe.Tests.Infrastructure;

public sealed class ExcludedApplicationRepositoryTests : SqliteTestBase
{
    private readonly ExcludedApplicationRepository _repository;

    public ExcludedApplicationRepositoryTests()
    {
        _repository = new ExcludedApplicationRepository(ConnectionFactory);
    }

    [Fact]
    public async Task AddAndIsExcludedAsync_DetectsExcludedProcess()
    {
        var app = ExcludedApplication.Create("KeePass.exe", "Gestor de contraseñas");
        await _repository.AddAsync(app);

        Assert.True(await _repository.IsExcludedAsync("KeePass"));
        Assert.True(await _repository.IsExcludedAsync("keepass.exe"));
        Assert.True(await _repository.IsExcludedAsync("KEEPASS"));
        Assert.False(await _repository.IsExcludedAsync("notepad.exe"));
    }

    [Fact]
    public async Task DeleteAsync_RemovesExclusionSuccessfully()
    {
        var app = ExcludedApplication.Create("cmd.exe");
        await _repository.AddAsync(app);

        Assert.True(await _repository.IsExcludedAsync("cmd"));

        await _repository.DeleteAsync(app.Id);

        Assert.False(await _repository.IsExcludedAsync("cmd"));
    }
}
