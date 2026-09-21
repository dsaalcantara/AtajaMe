using System.IO;
using AtajaMe.Application.Sync.Services;

namespace AtajaMe.Infrastructure.Sync;

/// <summary>
/// Implementación de IFileWatcherSyncService con FileSystemWatcher y debouncing para detectar actualizaciones de sync_manifest.json (RF-22, RF-23).
/// </summary>
public sealed class FileWatcherSyncService : IFileWatcherSyncService
{
    private const string ManifestFileName = "sync_manifest.json";
    private readonly string _syncDirectory;
    private readonly int _debounceMilliseconds;

    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private readonly object _lock = new();
    private bool _disposed;

    public event Func<Task>? RemoteManifestChanged;

    public bool IsRunning { get; private set; }

    public FileWatcherSyncService(string syncDirectory, int debounceMilliseconds = 500)
    {
        _syncDirectory = syncDirectory ?? throw new ArgumentNullException(nameof(syncDirectory));
        _debounceMilliseconds = debounceMilliseconds;
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_disposed || IsRunning)
            {
                return;
            }

            if (!Directory.Exists(_syncDirectory))
            {
                // Si la carpeta no existe, no rompemos: tolerancia a fallos (RF-23)
                return;
            }

            try
            {
                _watcher = new FileSystemWatcher(_syncDirectory)
                {
                    Filter = ManifestFileName,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };

                _watcher.Changed += OnFileSystemEvent;
                _watcher.Created += OnFileSystemEvent;
                _watcher.Renamed += OnFileSystemEvent;
                _watcher.Error += OnWatcherError;

                IsRunning = true;
            }
            catch
            {
                // Tolerante a fallos si la carpeta no admite watchers o no está disponible
                IsRunning = false;
            }
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (!IsRunning)
            {
                return;
            }

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Changed -= OnFileSystemEvent;
                _watcher.Created -= OnFileSystemEvent;
                _watcher.Renamed -= OnFileSystemEvent;
                _watcher.Error -= OnWatcherError;
                _watcher.Dispose();
                _watcher = null;
            }

            _debounceTimer?.Dispose();
            _debounceTimer = null;

            IsRunning = false;
        }
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        lock (_lock)
        {
            if (_disposed || !IsRunning)
            {
                return;
            }

            // Reiniciar temporizador de debouncing
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(async _ =>
            {
                try
                {
                    if (RemoteManifestChanged != null)
                    {
                        await RemoteManifestChanged.Invoke();
                    }
                }
                catch
                {
                    // Evitar que excepciones en el handler desestabilicen el proceso
                }
            }, null, _debounceMilliseconds, Timeout.Infinite);
        }
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        // El FileSystemWatcher notifica error por desconexión o desbordamiento de buffer
        lock (_lock)
        {
            Stop();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Stop();
        }
    }
}
