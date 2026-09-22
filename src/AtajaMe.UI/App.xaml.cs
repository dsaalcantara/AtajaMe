using System.IO;
using System.Windows;
using Microsoft.Win32;
using AtajaMe.Application.Backup.Services;
using AtajaMe.Application.Csv.Services;
using AtajaMe.Application.ExcludedApplications;
using AtajaMe.Application.Expansion;
using AtajaMe.Application.Folders;
using AtajaMe.Application.Services;
using AtajaMe.Application.Settings;
using AtajaMe.Application.Snippets;
using AtajaMe.Application.Sync.Services;
using AtajaMe.Application.Variables;
using AtajaMe.Infrastructure.Backup;
using AtajaMe.Infrastructure.Persistence;
using AtajaMe.Infrastructure.Repositories;
using AtajaMe.Infrastructure.Sync;
using AtajaMe.UI.Services;
using AtajaMe.UI.ViewModels;
using AtajaMe.UI.Views;
using AtajaMe.Windows.Focus;
using AtajaMe.Windows.Hooks;
using AtajaMe.Windows.Injection;
using AtajaMe.Windows.Native;
using AtajaMe.Windows.Orchestration;
using AtajaMe.Windows.Security;
using AtajaMe.Windows.Startup;

namespace AtajaMe.UI;

/// <summary>
/// Interaction logic for App.xaml.
/// Composition Root y orquestador del ciclo de vida en segundo plano (RF-34 y RF-35).
/// </summary>
public partial class App : System.Windows.Application
{
    private string _dbPath = string.Empty;
    private ISqliteConnectionFactory _connectionFactory = null!;
    private SqliteDatabaseInitializer _dbInitializer = null!;

    private SnippetRepository _snippetRepo = null!;
    private FolderRepository _folderRepo = null!;
    private ExcludedApplicationRepository _excludedAppsRepo = null!;
    private SyncMetadataRepository _syncMetadataRepo = null!;
    private AppSettingsRepository _appSettingsRepo = null!;
    private AppSettingsService _appSettingsService = null!;

    private GetSnippetsUseCase _getSnippetsUseCase = null!;
    private CreateSnippetUseCase _createSnippetUseCase = null!;
    private UpdateSnippetUseCase _updateSnippetUseCase = null!;
    private DeleteSnippetToTrashUseCase _deleteSnippetUseCase = null!;
    private RestoreSnippetUseCase _restoreSnippetUseCase = null!;
    private PurgeTrashUseCase _purgeTrashUseCase = null!;
    private DuplicateSnippetUseCase _duplicateSnippetUseCase = null!;
    private ToggleSnippetStateUseCase _toggleSnippetUseCase = null!;

    private GetFoldersUseCase _getFoldersUseCase = null!;
    private CreateFolderUseCase _createFolderUseCase = null!;
    private UpdateFolderUseCase _updateFolderUseCase = null!;
    private DeleteFolderUseCase _deleteFolderUseCase = null!;

    private ManageExcludedApplicationsUseCase _excludedAppsUseCase = null!;

    private VariableResolutionService _variableResolutionService = null!;
    private SnippetExpansionEngine _expansionEngine = null!;

    private DiskBackupStorage _backupStorage = null!;
    private SqliteBackupProvider _backupProvider = null!;
    private BackupService _backupService = null!;

    private CsvImportExportService _csvService = null!;

    private DiskSyncStorage _syncStorage = null!;
    private SyncEngine _syncEngine = null!;
    private FileWatcherSyncService _syncWatcher = null!;

    private IWin32Api _win32 = null!;
    private LowLevelKeyboardHook _keyboardHook = null!;
    private WindowsTextInjector _textInjector = null!;
    private SensitiveControlDetector _sensitiveDetector = null!;
    private FocusTracker _focusTracker = null!;
    private WindowsCaretLocator _caretLocator = null!;
    private WindowsStartupService _startupService = null!;
    private KeyboardExpansionOrchestrator _orchestrator = null!;

    private ThemeService _themeService = null!;
    private TrayIconService _trayIconService = null!;

    private MainViewModel _mainViewModel = null!;
    private MainWindow _mainWindow = null!;

    private bool _isExiting;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // 1. Inicialización de Almacenamiento y Esquema SQLite
            var appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AtajaMe");
            Directory.CreateDirectory(appDataDir);
            _dbPath = Path.Combine(appDataDir, "atajame.db");

            _connectionFactory = new SqliteConnectionFactory($"Data Source={_dbPath};Cache=Shared");
            _dbInitializer = new SqliteDatabaseInitializer(_connectionFactory);
            await _dbInitializer.InitializeAsync();

        // 2. Repositorios
        _snippetRepo = new SnippetRepository(_connectionFactory);
        _folderRepo = new FolderRepository(_connectionFactory);
        _excludedAppsRepo = new ExcludedApplicationRepository(_connectionFactory);
        _syncMetadataRepo = new SyncMetadataRepository(_connectionFactory);
        _appSettingsRepo = new AppSettingsRepository(_connectionFactory);
        _appSettingsService = new AppSettingsService(_appSettingsRepo);

        // 3. Casos de Uso
        _getSnippetsUseCase = new GetSnippetsUseCase(_snippetRepo);
        _createSnippetUseCase = new CreateSnippetUseCase(_snippetRepo, _folderRepo);
        _updateSnippetUseCase = new UpdateSnippetUseCase(_snippetRepo, _folderRepo);
        _deleteSnippetUseCase = new DeleteSnippetToTrashUseCase(_snippetRepo);
        _restoreSnippetUseCase = new RestoreSnippetUseCase(_snippetRepo);
        _purgeTrashUseCase = new PurgeTrashUseCase(_snippetRepo);
        _duplicateSnippetUseCase = new DuplicateSnippetUseCase(_snippetRepo);
        _toggleSnippetUseCase = new ToggleSnippetStateUseCase(_snippetRepo);

        _getFoldersUseCase = new GetFoldersUseCase(_folderRepo);
        _createFolderUseCase = new CreateFolderUseCase(_folderRepo);
        _updateFolderUseCase = new UpdateFolderUseCase(_folderRepo);
        _deleteFolderUseCase = new DeleteFolderUseCase(_folderRepo);

        _excludedAppsUseCase = new ManageExcludedApplicationsUseCase(_excludedAppsRepo);

        // 4. Motor de Expansión y Variables
        _variableResolutionService = new VariableResolutionService();
        _expansionEngine = new SnippetExpansionEngine(_snippetRepo, _variableResolutionService);

        // 5. Copias de Seguridad
        var backupsDir = Path.Combine(appDataDir, "Backups");
        _backupStorage = new DiskBackupStorage(backupsDir);
        _backupProvider = new SqliteBackupProvider(_connectionFactory);
        _backupService = new BackupService(_backupProvider, _backupStorage);

        // 6. CSV Import / Export
        _csvService = new CsvImportExportService(_snippetRepo, _folderRepo);

        // 7. Sincronización
        var syncDir = Path.Combine(appDataDir, "Sync");
        _syncStorage = new DiskSyncStorage(syncDir);
        _syncEngine = new SyncEngine(_snippetRepo, _folderRepo, _syncMetadataRepo, _syncStorage);
        _syncWatcher = new FileWatcherSyncService(syncDir);
        _syncWatcher.RemoteManifestChanged += async () => await _syncEngine.ProcessRemoteManifestAsync();
        _syncWatcher.Start();

        // 8. Servicios Win32
        _win32 = NativeWin32Api.Instance;
        _keyboardHook = new LowLevelKeyboardHook(_win32);
        _textInjector = new WindowsTextInjector(_win32);
        _sensitiveDetector = new SensitiveControlDetector();
        _focusTracker = new FocusTracker(_win32);
        _caretLocator = new WindowsCaretLocator(_win32);
        _startupService = new WindowsStartupService();

        _orchestrator = new KeyboardExpansionOrchestrator(
            _keyboardHook,
            _expansionEngine,
            _textInjector,
            _sensitiveDetector,
            _focusTracker,
            _excludedAppsRepo,
            _win32);

        // Suscripción al evento de solicitud de variables interactivas (RF-12)
        _orchestrator.InteractiveInputRequested += OnInteractiveInputRequested;

        // 9. Temas y Apariencia
        _themeService = new ThemeService();
        var settings = await _appSettingsService.GetSettingsAsync();
        _themeService.ApplyTheme(settings.Theme);

        // 10. Bandeja del Sistema (System Tray)
        _trayIconService = new TrayIconService(
            _orchestrator,
            openMainWindow: OpenMainWindow,
            openSettings: OpenSettingsDialog,
            syncNow: async () => await _syncEngine.ProcessRemoteManifestAsync(),
            exitApplication: ExitApplication);
        _trayIconService.Initialize();

        // 11. Ventana Principal y ViewModel
        _mainViewModel = new MainViewModel(
            _getSnippetsUseCase,
            _createSnippetUseCase,
            _updateSnippetUseCase,
            _deleteSnippetUseCase,
            _restoreSnippetUseCase,
            _purgeTrashUseCase,
            _duplicateSnippetUseCase,
            _toggleSnippetUseCase,
            _getFoldersUseCase,
            _createFolderUseCase,
            _updateFolderUseCase,
            _deleteFolderUseCase,
            _csvService,
            _orchestrator);

        _mainViewModel.RequestExportCsv += HandleExportCsv;
        _mainViewModel.RequestImportCsv += HandleImportCsv;

        _mainWindow = new MainWindow(_mainViewModel);

        // 12. Iniciar orquestador de teclado
        await _orchestrator.StartAsync();

        // Sembrado inicial de bienvenida si la biblioteca está vacía
        var existingSnippets = await _snippetRepo.GetAllActiveAsync();
        if (!existingSnippets.Any())
        {
            var generalFolder = AtajaMe.Domain.Entities.Folder.Create("General");
            await _folderRepo.AddAsync(generalFolder);

            var welcomeSnippet = AtajaMe.Domain.Entities.Snippet.Create(
                ":hola",
                "Bienvenida a AtajaMe",
                "¡Hola! Te damos la bienvenida a AtajaMe. Hoy es {fecha} y son las {hora}.",
                generalFolder.Id);

            var interactiveSnippet = AtajaMe.Domain.Entities.Snippet.Create(
                ":saludo",
                "Saludo Interactivo",
                "Hola {nombre}, un gusto saludarte. Hoy es {fecha}.",
                generalFolder.Id);

            await _snippetRepo.AddAsync(welcomeSnippet);
            await _snippetRepo.AddAsync(interactiveSnippet);

            await _mainViewModel.LoadDataAsync();
        }

        // 13. Mostrar ventana salvo que se indique arranque en segundo plano / minimizado
        bool startMinimized = settings.StartMinimized ||
                              e.Args.Any(a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase) ||
                                              a.Equals("/background", StringComparison.OrdinalIgnoreCase));

        if (!startMinimized)
        {
            _mainWindow.Show();
        }

        // Ciclo de vida persistente en segundo plano (RF-34, RF-35)
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }
    catch (Exception ex)
    {
        System.Windows.MessageBox.Show(
            $"Ocurrió un error al iniciar AtajaMe:\n\n{ex.Message}\n\nDetalles:\n{ex.StackTrace}",
            "Error en AtajaMe",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Error);

        Shutdown(1);
    }
}

    private void OnInteractiveInputRequested(object? sender, InteractiveInputRequestedEventArgs e)
    {
        Dispatcher.InvokeAsync(async () =>
        {
            if (e.Result.Snippet == null)
            {
                return;
            }

            var vm = new FloatingVariableViewModel(
                e.Result.Snippet.Code,
                e.Result.Snippet.Name,
                e.Result.PendingInputs.Select(v => v.Name));

            var dialog = new FloatingVariableDialog(vm, e.WindowHandle, _caretLocator);
            bool? confirmed = dialog.ShowDialog();

            if (confirmed == true && vm.IsConfirmed)
            {
                var values = vm.GetValues();
                await _orchestrator.CompleteInteractiveExpansionAsync(e.Result, values, e.WindowHandle);
            }
        });
    }

    public void OpenMainWindow()
    {
        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        _mainWindow.Show();
        _mainWindow.Activate();
        _mainWindow.Focus();
    }

    public void OpenSettingsDialog()
    {
        var vm = new SettingsViewModel(
            _appSettingsService,
            _startupService,
            _themeService,
            _backupService,
            _syncEngine,
            _excludedAppsUseCase);

        var dialog = new SettingsDialog(vm)
        {
            Owner = _mainWindow.IsVisible ? _mainWindow : null
        };
        dialog.ShowDialog();
    }

    public void OpenConflictResolutionDialog(Window? owner = null)
    {
        var vm = new ConflictResolutionViewModel(_syncEngine);
        var dialog = new ConflictResolutionDialog(vm)
        {
            Owner = owner ?? (_mainWindow.IsVisible ? _mainWindow : null)
        };
        dialog.ShowDialog();
    }

    private async void HandleExportCsv()
    {
        var sfd = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Archivos CSV (*.csv)|*.csv",
            FileName = "AtajaMe_Atajos.csv",
            DefaultExt = ".csv"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                await using var stream = sfd.OpenFile();
                await using var writer = new StreamWriter(stream);
                await _csvService.ExportAllAsync(writer);
                _mainViewModel.StatusMessage = "Biblioteca exportada a CSV con éxito.";
            }
            catch (Exception ex)
            {
                _mainViewModel.StatusMessage = $"Error al exportar CSV: {ex.Message}";
            }
        }
    }

    private async void HandleImportCsv()
    {
        var ofd = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Archivos CSV (*.csv)|*.csv",
            DefaultExt = ".csv"
        };

        if (ofd.ShowDialog() == true)
        {
            try
            {
                using (var reader = new StreamReader(ofd.FileName))
                {
                    var preview = await _csvService.AnalyzeImportAsync(reader);
                    if (preview.ConflictCount > 0 || preview.InvalidRows > 0)
                    {
                        var previewVm = new CsvImportPreviewViewModel(preview);
                        var previewDialog = new CsvImportPreviewDialog(previewVm)
                        {
                            Owner = _mainWindow
                        };

                        if (previewDialog.ShowDialog() != true || !previewVm.IsConfirmed)
                        {
                            _mainViewModel.StatusMessage = "Importación CSV cancelada.";
                            return;
                        }

                        using var execReader = new StreamReader(ofd.FileName);
                        var result = await _csvService.ExecuteImportAsync(
                            execReader,
                            previewVm.GetResolutionOverrides(),
                            previewVm.DefaultResolution);

                        await _mainViewModel.LoadDataAsync();
                        _mainViewModel.StatusMessage = $"Importación completada: {result.ImportedCount} importados, {result.OverwrittenCount} sobrescritos.";
                        return;
                    }
                }

                // Sin colisiones ni errores
                using (var execReader = new StreamReader(ofd.FileName))
                {
                    var result = await _csvService.ExecuteImportAsync(execReader);
                    await _mainViewModel.LoadDataAsync();
                    _mainViewModel.StatusMessage = $"Importación completada: {result.ImportedCount} atajos importados.";
                }
            }
            catch (Exception ex)
            {
                _mainViewModel.StatusMessage = $"Error al importar CSV: {ex.Message}";
            }
        }
    }

    public async void ExitApplication()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;

        try
        {
            await _orchestrator.StopAsync();
            _syncWatcher.Dispose();
            _trayIconService.Dispose();
            _mainWindow.ForceClose();
        }
        catch
        {
            // Ignorar errores durante el apagado
        }
        finally
        {
            Shutdown();
        }
    }
}
