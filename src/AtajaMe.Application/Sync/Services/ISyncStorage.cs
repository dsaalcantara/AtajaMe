using AtajaMe.Application.Sync.DTOs;

namespace AtajaMe.Application.Sync.Services;

/// <summary>
/// Contrato para almacenamiento y acceso atómico a los artefactos de sincronización en disco/nube.
/// </summary>
public interface ISyncStorage
{
    /// <summary>
    /// Comprueba si la carpeta compartida existe y es accesible actualmente (RF-23).
    /// </summary>
    bool IsAvailable();

    /// <summary>
    /// Obtiene la ruta del directorio configurado para la sincronización.
    /// </summary>
    string GetSyncDirectory();

    /// <summary>
    /// Lee y deserializa el paquete de sincronización sync_manifest.json si existe.
    /// </summary>
    Task<SyncManifestDto?> ReadManifestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Escribe de forma atómica el paquete sync_manifest.json en la carpeta compartida (vía archivo temporal + reemplazo).
    /// </summary>
    Task WriteManifestAsync(SyncManifestDto manifest, CancellationToken cancellationToken = default);
}
