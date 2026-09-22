using AtajaMe.Application.Settings;
using AtajaMe.Domain.Repositories;

namespace AtajaMe.Tests.Application;

public sealed class AppSettingsServiceTests
{
    private readonly FakeAppSettingsRepository _fakeRepo = new();
    private readonly AppSettingsService _service;

    public AppSettingsServiceTests()
    {
        _service = new AppSettingsService(_fakeRepo);
    }

    [Fact]
    public async Task GetSettingsAsync_WhenEmpty_ReturnsDefaultSettings()
    {
        var settings = await _service.GetSettingsAsync();

        Assert.Equal("System", settings.Theme);
        Assert.Null(settings.SyncFolderPath);
        Assert.False(settings.SyncEnabled);
        Assert.False(settings.StartWithWindows);
        Assert.False(settings.StartMinimized);
        Assert.Equal(10, settings.BackupRetentionCount);
        Assert.Equal(10, settings.RecentSnippetsLimit);
        Assert.Equal(30, settings.TrashRetentionDays);
    }

    [Fact]
    public async Task SaveSettingsAsync_PersistsAllSettingsAndReadsBack()
    {
        var toSave = new AppSettingsDto(
            Theme: "Dark",
            SyncFolderPath: @"C:\CloudSync",
            SyncEnabled: true,
            StartWithWindows: true,
            StartMinimized: true,
            BackupRetentionCount: 15,
            RecentSnippetsLimit: 20,
            TrashRetentionDays: 60);

        await _service.SaveSettingsAsync(toSave);

        var retrieved = await _service.GetSettingsAsync();

        Assert.Equal("Dark", retrieved.Theme);
        Assert.Equal(@"C:\CloudSync", retrieved.SyncFolderPath);
        Assert.True(retrieved.SyncEnabled);
        Assert.True(retrieved.StartWithWindows);
        Assert.True(retrieved.StartMinimized);
        Assert.Equal(15, retrieved.BackupRetentionCount);
        Assert.Equal(20, retrieved.RecentSnippetsLimit);
        Assert.Equal(60, retrieved.TrashRetentionDays);
    }

    private sealed class FakeAppSettingsRepository : IAppSettingsRepository
    {
        private readonly Dictionary<string, string> _storage = new();

        public Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
        {
            _storage.TryGetValue(key, out var val);
            return Task.FromResult<string?>(val);
        }

        public Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            _storage[key] = value;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>>(_storage);
        }

        public Task DeleteKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            _storage.Remove(key);
            return Task.CompletedTask;
        }
    }
}
