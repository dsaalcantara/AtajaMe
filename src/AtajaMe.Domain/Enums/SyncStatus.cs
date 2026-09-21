namespace AtajaMe.Domain.Enums;

/// <summary>
/// Estado operativo del motor de sincronización (RF-22, RF-23).
/// </summary>
public enum SyncStatus
{
    /// <summary>
    /// Sincronización inactiva o esperando eventos.
    /// </summary>
    Idle = 0,

    /// <summary>
    /// Proceso de sincronización en curso.
    /// </summary>
    Syncing = 1,

    /// <summary>
    /// Sincronización completada con éxito sin incidencias.
    /// </summary>
    Success = 2,

    /// <summary>
    /// Sincronización completada pero se han detectado conflictos que requieren resolución del usuario.
    /// </summary>
    ConflictsDetected = 3,

    /// <summary>
    /// La carpeta compartida no está disponible (sin conexión, disco desmontado o ruta no encontrada). (RF-23).
    /// </summary>
    Unavailable = 4,

    /// <summary>
    /// Error durante el ciclo de sincronización (permisos, archivo corrupto, etc.).
    /// </summary>
    Error = 5
}
