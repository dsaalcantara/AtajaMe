namespace AtajaMe.Application.Sync.Services;

/// <summary>
/// Contrato del monitor de cambios en segundo plano para la carpeta compartida (RF-22).
/// </summary>
public interface IFileWatcherSyncService : IDisposable
{
    /// <summary>
    /// Evento disparado cuando se detecta una modificación externa en sync_manifest.json (con debouncing aplicado).
    /// </summary>
    event Func<Task>? RemoteManifestChanged;

    /// <summary>
    /// Indica si el monitor está activo.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Inicia la vigilancia de la carpeta compartida.
    /// </summary>
    void Start();

    /// <summary>
    /// Detiene la vigilancia de la carpeta compartida.
    /// </summary>
    void Stop();
}
