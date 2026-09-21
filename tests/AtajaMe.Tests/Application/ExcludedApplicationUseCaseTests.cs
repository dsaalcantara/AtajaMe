using AtajaMe.Application.ExcludedApplications;
using AtajaMe.Infrastructure.Repositories;
using AtajaMe.Tests.Infrastructure;

namespace AtajaMe.Tests.Application;

public sealed class ExcludedApplicationUseCaseTests : SqliteTestBase
{
    private readonly ExcludedApplicationRepository _repository;
    private readonly ManageExcludedApplicationsUseCase _useCase;

    public ExcludedApplicationUseCaseTests()
    {
        _repository = new ExcludedApplicationRepository(ConnectionFactory);
        _useCase = new ManageExcludedApplicationsUseCase(_repository);
    }

    [Fact]
    public async Task ManageExcludedApplications_AddListCheckRemove_FlowWorks()
    {
        var added = await _useCase.AddAsync("1Password.exe", "Gestor de claves");

        Assert.Equal("1Password", added.ProcessName);
        Assert.Equal("Gestor de claves", added.Description);

        var list = await _useCase.GetAllAsync();
        Assert.Single(list);

        var isExcluded = await _useCase.IsExcludedAsync("1password");
        Assert.True(isExcluded);

        await _useCase.RemoveAsync(added.Id);

        var isStillExcluded = await _useCase.IsExcludedAsync("1password");
        Assert.False(isStillExcluded);
    }
}
