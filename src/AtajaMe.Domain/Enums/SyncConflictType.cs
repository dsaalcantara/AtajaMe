namespace AtajaMe.Domain.Enums;

/// <summary>
/// Tipo de conflicto detectado durante el proceso de sincronización.
/// </summary>
public enum SyncConflictType
{
    /// <summary>
    /// Modificación concurrente: tanto la versión local como la remota cambiaron posteriormente al último sync con contenido dispar.
    /// </summary>
    ConcurrentModification = 0,

    /// <summary>
    /// Colisión de código: el registro remoto utiliza un código que ya está en uso por otro atajo local diferente.
    /// </summary>
    CodeCollision = 1,

    /// <summary>
    /// El atajo fue modificado localmente pero eliminado de forma remota.
    /// </summary>
    ModifiedLocallyDeletedRemotely = 2
}
