using System.Collections.ObjectModel;
using System.Windows.Input;
using AtajaMe.Application.Backup.DTOs;
using AtajaMe.Application.Backup.Services;
using AtajaMe.Application.DTOs;
using AtajaMe.Application.ExcludedApplications;
using AtajaMe.Application.Services;
using AtajaMe.Application.Settings;
using AtajaMe.Application.Sync.Services;
using AtajaMe.UI.Common;
using AtajaMe.UI.Services;

namespace AtajaMe.UI.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly AppSettingsService _settingsService;
    private readonly IWindowsStartupService _startupService;
    private readonly IThemeService _themeService;
    private readonly IBackupService _backupService;
    private readonly ISyncEngine _syncEngine;
    private readonly ManageExcludedApplicationsUseCase _excludedAppsUseCase;

    private string _theme = "System";
    private bool _startWithWindows;
    private bool _startMinimized;
    private bool _syncEnabled;
    private string? _syncFolderPath;
    private int _backupRetentionCount = 10;
    private int _recentSnippetsLimit = 10;
    private int _trashRetentionDays = 30;

    private BackupFileInfoDto? _selectedBackup;
    private string _newExcludedProcessName = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _hasPendingConflicts;

    public string Theme
    {
        get => _theme;
        set
        {
            if (SetProperty(ref _theme, value))
            {
                _themeService.ApplyTheme(value);
            }
        }
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set => SetProperty(ref _startWithWindows, value);
    }

    public bool StartMinimized
    {
        get => _startMinimized;
        set => SetProperty(ref _startMinimized, value);
    }

    public bool SyncEnabled
    {
        get => _syncEnabled;
        set => SetProperty(ref _syncEnabled, value);
    }

    public string? SyncFolderPath
    {
        get => _syncFolderPath;
        set => SetProperty(ref _syncFolderPath, value);
    }

    public int BackupRetentionCount
    {
        get => _backupRetentionCount;
        set => SetProperty(ref _backupRetentionCount, value);
    }

    public int RecentSnippetsLimit
    {
        get => _recentSnippetsLimit;
        set => SetProperty(ref _recentSnippetsLimit, value);
    }

    public int TrashRetentionDays
    {
        get => _trashRetentionDays;
        set => SetProperty(ref _trashRetentionDays, value);
    }

    public BackupFileInfoDto? SelectedBackup
    {
        get => _selectedBackup;
        set => SetProperty(ref _selectedBackup, value);
    }

    public string NewExcludedProcessName
    {
        get => _newExcludedProcessName;
        set => SetProperty(ref _newExcludedProcessName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool HasPendingConflicts
    {
        get => _hasPendingConflicts;
        set => SetProperty(ref _hasPendingConflicts, value);
    }

    public ObservableCollection<ExcludedApplicationDto> ExcludedApps { get; } = new();
    public ObservableCollection<BackupFileInfoDto> Backups { get; } = new();

    public ICommand SaveSettingsCommand { get; }
    public ICommand CreateBackupNowCommand { get; }
    public ICommand RestoreBackupCommand { get; }
    public ICommand SyncNowCommand { get; }
    public ICommand AddExcludedAppCommand { get; }
    public ICommand RemoveExcludedAppCommand { get; }

    public event Action? RequestClose;
    public event Action? RequestOpenConflictResolution;

    public SettingsViewModel(
        AppSettingsService settingsService,
        IWindowsStartupService startupService,
        IThemeService themeService,
        IBackupService backupService,
        ISyncEngine syncEngine,
        ManageExcludedApplicationsUseCase excludedAppsUseCase)
    {
        _settingsService = settingsService;
        _startupService = startupService;
        _themeService = themeService;
        _backupService = backupService;
        _syncEngine = syncEngine;
        _excludedAppsUseCase = excludedAppsUseCase;

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        CreateBackupNowCommand = new AsyncRelayCommand(CreateBackupNowAsync);
        RestoreBackupCommand = new AsyncRelayCommand(RestoreSelectedBackupAsync, () => SelectedBackup != null);
        SyncNowCommand = new AsyncRelayCommand(SyncNowAsync);
        AddExcludedAppCommand = new AsyncRelayCommand(AddExcludedAppAsync);
        RemoveExcludedAppCommand = new AsyncRelayCommand(RemoveExcludedAppAsync);
    }

    public async Task InitializeAsync()
    {
        var settings = await _settingsService.GetSettingsAsync();
        _theme = settings.Theme;
        _syncFolderPath = settings.SyncFolderPath;
        _syncEnabled = settings.SyncEnabled;
        _startWithWindows = _startupService.IsStartupEnabled();
        _startMinimized = settings.StartMinimized;
        _backupRetentionCount = settings.BackupRetentionCount;
        _recentSnippetsLimit = settings.RecentSnippetsLimit;
        _trashRetentionDays = settings.TrashRetentionDays;

        OnPropertyChanged(nameof(Theme));
        OnPropertyChanged(nameof(SyncFolderPath));
        OnPropertyChanged(nameof(SyncEnabled));
        OnPropertyChanged(nameof(StartWithWindows));
        OnPropertyChanged(nameof(StartMinimized));
        OnPropertyChanged(nameof(BackupRetentionCount));
        OnPropertyChanged(nameof(RecentSnippetsLimit));
        OnPropertyChanged(nameof(TrashRetentionDays));

        await LoadExcludedAppsAsync();
        await LoadBackupsAsync();
        await CheckConflictsAsync();
    }

    public async Task SaveSettingsAsync()
    {
        try
        {
            var dto = new AppSettingsDto(
                Theme,
                SyncFolderPath,
                SyncEnabled,
                StartWithWindows,
                StartMinimized,
                BackupRetentionCount,
                RecentSnippetsLimit,
                TrashRetentionDays);

            await _settingsService.SaveSettingsAsync(dto);
            _startupService.SetStartup(StartWithWindows, StartMinimized);
            StatusMessage = "Configuración guardada correctamente.";
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al guardar configuración: {ex.Message}";
        }
    }

    public async Task LoadExcludedAppsAsync()
    {
        var apps = await _excludedAppsUseCase.GetAllAsync();
        ExcludedApps.Clear();
        foreach (var app in apps)
        {
            ExcludedApps.Add(app);
        }
    }

    public async Task AddExcludedAppAsync()
    {
        if (string.IsNullOrWhiteSpace(NewExcludedProcessName))
        {
            return;
        }

        try
        {
            await _excludedAppsUseCase.AddAsync(NewExcludedProcessName.Trim());
            NewExcludedProcessName = string.Empty;
            await LoadExcludedAppsAsync();
            StatusMessage = "Aplicación excluida añadida.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    public async Task RemoveExcludedAppAsync(object? parameter)
    {
        if (parameter is ExcludedApplicationDto app)
        {
            try
            {
                await _excludedAppsUseCase.RemoveAsync(app.Id);
                await LoadExcludedAppsAsync();
                StatusMessage = "Exclusión eliminada.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }
    }

    public async Task LoadBackupsAsync()
    {
        try
        {
            var list = await _backupService.ListBackupsAsync();
            Backups.Clear();
            foreach (var b in list)
            {
                Backups.Add(b);
            }
        }
        catch
        {
            // Ignorar errores si el directorio de backups está vacío
        }
    }

    public async Task CreateBackupNowAsync()
    {
        try
        {
            var created = await _backupService.CreateBackupAsync();
            await LoadBackupsAsync();
            StatusMessage = $"Copia creada: {created.FileName} ({created.FileSizeBytes / 1024:N0} KB)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al crear copia: {ex.Message}";
        }
    }

    public async Task RestoreSelectedBackupAsync()
    {
        if (SelectedBackup == null)
        {
            return;
        }

        try
        {
            await _backupService.RestoreBackupAsync(SelectedBackup.FilePath);
            StatusMessage = $"Copia {SelectedBackup.FileName} restaurada con éxito. Reinicia la aplicación para aplicar todos los cambios.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al restaurar: {ex.Message}";
        }
    }

    public async Task SyncNowAsync()
    {
        try
        {
            StatusMessage = "Sincronizando con carpeta remota...";
            var result = await _syncEngine.ProcessRemoteManifestAsync();
            if (result.Success)
            {
                StatusMessage = $"Sincronización completada: {result.AddedLocallyCount} añadidos, {result.UpdatedLocallyCount} actualizados.";
            }
            else
            {
                StatusMessage = $"Aviso de sincronización: {result.Message}";
            }

            await CheckConflictsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error de sincronización: {ex.Message}";
        }
    }

    public async Task CheckConflictsAsync()
    {
        var conflicts = await _syncEngine.GetPendingConflictsAsync();
        HasPendingConflicts = conflicts.Count > 0;
    }

    public void OpenConflictResolution()
    {
        RequestOpenConflictResolution?.Invoke();
    }
}
