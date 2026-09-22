using AtajaMe.Infrastructure.Repositories;

namespace AtajaMe.Tests.Infrastructure;

public sealed class AppSettingsRepositoryTests : SqliteTestBase
{
    private readonly AppSettingsRepository _repository;

    public AppSettingsRepositoryTests()
    {
        _repository = new AppSettingsRepository(ConnectionFactory);
    }

    [Fact]
    public async Task SetValueAsync_And_GetValueAsync_ReturnsPersistedValue()
    {
        await _repository.SetValueAsync("Theme", "Dark");

        var value = await _repository.GetValueAsync("Theme");

        Assert.Equal("Dark", value);
    }

    [Fact]
    public async Task SetValueAsync_WhenKeyExists_UpdatesValue()
    {
        await _repository.SetValueAsync("Theme", "Light");
        await _repository.SetValueAsync("Theme", "Dark");

        var value = await _repository.GetValueAsync("Theme");

        Assert.Equal("Dark", value);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllStoredSettings()
    {
        await _repository.SetValueAsync("K1", "V1");
        await _repository.SetValueAsync("K2", "V2");

        var all = await _repository.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Equal("V1", all["K1"]);
        Assert.Equal("V2", all["K2"]);
    }

    [Fact]
    public async Task DeleteKeyAsync_RemovesKeyFromDatabase()
    {
        await _repository.SetValueAsync("Temp", "123");
        await _repository.DeleteKeyAsync("Temp");

        var value = await _repository.GetValueAsync("Temp");

        Assert.Null(value);
    }
}
