using AtajaMe.Application.Backup.DTOs;
using AtajaMe.Application.Backup.Services;
using AtajaMe.Application.DTOs;
using AtajaMe.Application.ExcludedApplications;
using AtajaMe.Application.Services;
using AtajaMe.Application.Settings;
using AtajaMe.Application.Sync.DTOs;
using AtajaMe.Application.Sync.Services;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Enums;
using AtajaMe.Domain.Repositories;
using AtajaMe.Domain.Updates;
using AtajaMe.Application.Updates;
using AtajaMe.UI.Services;
using AtajaMe.UI.ViewModels;

namespace AtajaMe.Tests.UI;

public sealed class SettingsViewModelTests
{
    private readonly FakeAppSettingsRepo _appSettingsRepo = new();
    private readonly AppSettingsService _settingsService;
    private readonly FakeStartupService _startupService = new();
    private readonly FakeThemeService _themeService = new();
    private readonly FakeBackupService _backupService = new();
    private readonly FakeSyncEngine _syncEngine = new();
    private readonly FakeExcludedAppRepo _excludedRepo = new();
    private readonly ManageExcludedApplicationsUseCase _excludedUseCase;
    private readonly FakeUpdateChecker _fakeChecker = new();
    private readonly CheckForUpdatesUseCase _checkForUpdatesUseCase;
    private readonly FakeUpdateDownloader _fakeDownloader = new();

    public SettingsViewModelTests()
    {
        _settingsService = new AppSettingsService(_appSettingsRepo);
        _excludedUseCase = new ManageExcludedApplicationsUseCase(_excludedRepo);
        _checkForUpdatesUseCase = new CheckForUpdatesUseCase(_fakeChecker);
    }

    [Fact]
    public async Task InitializeAsync_LoadsSettingsAndExcludedApps()
    {
        await _settingsService.SaveSettingsAsync(new AppSettingsDto("Dark", @"C:\Sync", true, true, false, 10, 10, 30));
        await _excludedRepo.AddAsync(ExcludedApplication.Create("KeePass.exe"));

        var vm = new SettingsViewModel(
            _settingsService,
            _startupService,
            _themeService,
            _backupService,
            _syncEngine,
            _excludedUseCase,
            _checkForUpdatesUseCase,
            _fakeDownloader);

        await vm.InitializeAsync();

        Assert.Equal("Dark", vm.Theme);
        Assert.Equal(@"C:\Sync", vm.SyncFolderPath);
        Assert.True(vm.SyncEnabled);
        Assert.Single(vm.ExcludedApps);
        Assert.Equal("KeePass", vm.ExcludedApps[0].ProcessName);
    }

    [Fact]
    public async Task SaveSettingsCommand_SavesSettingsAndCloses()
    {
        var vm = new SettingsViewModel(
            _settingsService,
            _startupService,
            _themeService,
            _backupService,
            _syncEngine,
            _excludedUseCase,
            _checkForUpdatesUseCase,
            _fakeDownloader);

        await vm.InitializeAsync();

        bool closed = false;
        vm.RequestClose += () => closed = true;

        vm.Theme = "Dark";
        vm.StartWithWindows = true;

        await vm.SaveSettingsAsync();

        Assert.True(closed);
        Assert.True(_startupService.IsStartupEnabled());
        var saved = await _settingsService.GetSettingsAsync();
        Assert.Equal("Dark", saved.Theme);
    }

    [Fact]
    public async Task AddExcludedAppCommand_AddsApplicationToList()
    {
        var vm = new SettingsViewModel(
            _settingsService,
            _startupService,
            _themeService,
            _backupService,
            _syncEngine,
            _excludedUseCase,
            _checkForUpdatesUseCase,
            _fakeDownloader);

        await vm.InitializeAsync();

        vm.NewExcludedProcessName = "mstsc.exe";
        await vm.AddExcludedAppAsync();

        Assert.Single(vm.ExcludedApps);
        Assert.Equal("mstsc", vm.ExcludedApps[0].ProcessName);
    }

    #region Fakes

    private sealed class FakeAppSettingsRepo : IAppSettingsRepository
    {
        private readonly Dictionary<string, string> _dict = new();
        public Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
        {
            _dict.TryGetValue(key, out var val);
            return Task.FromResult<string?>(val);
        }
        public Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            _dict[key] = value;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<string, string>>(_dict);
        public Task DeleteKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            _dict.Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStartupService : IWindowsStartupService
    {
        private bool _enabled;
        public bool IsStartupEnabled() => _enabled;
        public void SetStartup(bool enable, bool startMinimized = false) => _enabled = enable;
    }

    private sealed class FakeThemeService : IThemeService
    {
        public string CurrentTheme { get; private set; } = "System";
        public bool IsDarkActive => CurrentTheme == "Dark";
        public void ApplyTheme(string themePreference) => CurrentTheme = themePreference;
    }

    private sealed class FakeBackupService : IBackupService
    {
        public Task<BackupFileInfoDto> CreateBackupAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new BackupFileInfoDto("path", "file.db", 1024, DateTime.UtcNow));
        public Task<IReadOnlyList<BackupFileInfoDto>> ListBackupsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BackupFileInfoDto>>(Array.Empty<BackupFileInfoDto>());
        public Task<BackupPreviewDto> GetBackupPreviewAsync(string backupFilePath, CancellationToken cancellationToken = default)
            => Task.FromResult(new BackupPreviewDto(backupFilePath, "file.db", 1024, DateTime.UtcNow, 0, 0, true));
        public Task RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task ApplyRetentionPolicyAsync(int maxCopiesToKeep = 10, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeSyncEngine : ISyncEngine
    {
        public Task<SyncManifestDto> ExportManifestAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<SyncResult> ProcessRemoteManifestAsync(CancellationToken cancellationToken = default) => Task.FromResult(SyncResult.Succeeded());
        public Task ResolveConflictAsync(SyncConflict conflict, SyncResolutionChoice choice, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<SyncConflict>> GetPendingConflictsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SyncConflict>>(Array.Empty<SyncConflict>());
    }

    private sealed class FakeExcludedAppRepo : IExcludedApplicationRepository
    {
        private readonly List<ExcludedApplication> _apps = new();
        public Task<IReadOnlyList<ExcludedApplication>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ExcludedApplication>>(_apps);
        public Task<bool> IsExcludedAsync(string processName, CancellationToken cancellationToken = default)
            => Task.FromResult(_apps.Any(a => string.Equals(a.ProcessName, processName, StringComparison.OrdinalIgnoreCase)));
        public Task AddAsync(ExcludedApplication application, CancellationToken cancellationToken = default)
        {
            _apps.Add(application);
            return Task.CompletedTask;
        }
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _apps.RemoveAll(a => a.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUpdateChecker : IUpdateChecker
    {
        public UpdateInfo? UpdateToReturn { get; set; }
        public Task<UpdateInfo?> CheckForUpdatesAsync(Version currentVersion, CancellationToken cancellationToken = default)
            => Task.FromResult(UpdateToReturn);
    }

    private sealed class FakeUpdateDownloader : IUpdateDownloader
    {
        public string ResultPath { get; set; } = @"C:\Temp\AtajaMe_Setup.exe";
        public Task<string> DownloadInstallerAsync(string downloadUrl, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            progress?.Report(1.0);
            return Task.FromResult(ResultPath);
        }
    }

    #endregion
}
