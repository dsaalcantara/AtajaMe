using AtajaMe.Domain.Repositories;

namespace AtajaMe.Application.Settings;

public sealed record AppSettingsDto(
    string Theme,
    string? SyncFolderPath,
    bool SyncEnabled,
    bool StartWithWindows,
    bool StartMinimized,
    int BackupRetentionCount,
    int RecentSnippetsLimit,
    int TrashRetentionDays,
    string DefaultDateFormat = "dd/MM/yyyy",
    string DefaultTimeFormat = "HH:mm",
    bool CheckUpdatesOnStartup = true)
{
    public static AppSettingsDto Default => new(
        Theme: "System",
        SyncFolderPath: null,
        SyncEnabled: false,
        StartWithWindows: false,
        StartMinimized: false,
        BackupRetentionCount: 10,
        RecentSnippetsLimit: 10,
        TrashRetentionDays: 30,
        DefaultDateFormat: "dd/MM/yyyy",
        DefaultTimeFormat: "HH:mm",
        CheckUpdatesOnStartup: true);
}

public sealed class AppSettingsService
{
    public const string KeyTheme = "AppTheme";
    public const string KeySyncFolderPath = "SyncFolderPath";
    public const string KeySyncEnabled = "SyncEnabled";
    public const string KeyStartWithWindows = "StartWithWindows";
    public const string KeyStartMinimized = "StartMinimized";
    public const string KeyBackupRetention = "BackupRetentionCount";
    public const string KeyRecentLimit = "RecentSnippetsLimit";
    public const string KeyTrashRetention = "TrashRetentionDays";
    public const string KeyDefaultDateFormat = "DefaultDateFormat";
    public const string KeyDefaultTimeFormat = "DefaultTimeFormat";
    public const string KeyCheckUpdatesOnStartup = "CheckUpdatesOnStartup";

    private readonly IAppSettingsRepository _repository;

    public AppSettingsService(IAppSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<AppSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var dict = await _repository.GetAllAsync(cancellationToken);

        var theme = dict.TryGetValue(KeyTheme, out var t) ? t : "System";
        dict.TryGetValue(KeySyncFolderPath, out var syncFolder);
        var syncEnabled = dict.TryGetValue(KeySyncEnabled, out var se) && bool.TryParse(se, out var seb) && seb;
        var startWindows = dict.TryGetValue(KeyStartWithWindows, out var sw) && bool.TryParse(sw, out var swb) && swb;
        var startMin = dict.TryGetValue(KeyStartMinimized, out var sm) && bool.TryParse(sm, out var smb) && smb;
        var backupRetention = dict.TryGetValue(KeyBackupRetention, out var br) && int.TryParse(br, out var bri) ? bri : 10;
        var recentLimit = dict.TryGetValue(KeyRecentLimit, out var rl) && int.TryParse(rl, out var rli) ? rli : 10;
        var trashRetention = dict.TryGetValue(KeyTrashRetention, out var tr) && int.TryParse(tr, out var tri) ? tri : 30;
        var dateFormat = dict.TryGetValue(KeyDefaultDateFormat, out var df) && !string.IsNullOrWhiteSpace(df) ? df : "dd/MM/yyyy";
        var timeFormat = dict.TryGetValue(KeyDefaultTimeFormat, out var tf) && !string.IsNullOrWhiteSpace(tf) ? tf : "HH:mm";
        var checkUpdates = !dict.TryGetValue(KeyCheckUpdatesOnStartup, out var cu) || !bool.TryParse(cu, out var cub) || cub;

        return new AppSettingsDto(
            theme,
            syncFolder,
            syncEnabled,
            startWindows,
            startMin,
            backupRetention,
            recentLimit,
            trashRetention,
            dateFormat,
            timeFormat,
            checkUpdates);
    }

    public async Task SaveSettingsAsync(AppSettingsDto settings, CancellationToken cancellationToken = default)
    {
        await _repository.SetValueAsync(KeyTheme, settings.Theme, cancellationToken);
        if (settings.SyncFolderPath != null)
        {
            await _repository.SetValueAsync(KeySyncFolderPath, settings.SyncFolderPath, cancellationToken);
        }
        else
        {
            await _repository.DeleteKeyAsync(KeySyncFolderPath, cancellationToken);
        }

        await _repository.SetValueAsync(KeySyncEnabled, settings.SyncEnabled.ToString(), cancellationToken);
        await _repository.SetValueAsync(KeyStartWithWindows, settings.StartWithWindows.ToString(), cancellationToken);
        await _repository.SetValueAsync(KeyStartMinimized, settings.StartMinimized.ToString(), cancellationToken);
        await _repository.SetValueAsync(KeyBackupRetention, settings.BackupRetentionCount.ToString(), cancellationToken);
        await _repository.SetValueAsync(KeyRecentLimit, settings.RecentSnippetsLimit.ToString(), cancellationToken);
        await _repository.SetValueAsync(KeyTrashRetention, settings.TrashRetentionDays.ToString(), cancellationToken);
        await _repository.SetValueAsync(KeyDefaultDateFormat, settings.DefaultDateFormat, cancellationToken);
        await _repository.SetValueAsync(KeyDefaultTimeFormat, settings.DefaultTimeFormat, cancellationToken);
        await _repository.SetValueAsync(KeyCheckUpdatesOnStartup, settings.CheckUpdatesOnStartup.ToString(), cancellationToken);
    }
}
