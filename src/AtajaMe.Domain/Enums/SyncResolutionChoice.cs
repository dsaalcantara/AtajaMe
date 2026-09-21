namespace AtajaMe.Domain.Enums;

/// <summary>
/// Opciones de resolución de un conflicto de sincronización (RF-24).
/// </summary>
public enum SyncResolutionChoice
{
    /// <summary>
    /// Conserva la versión local del atajo (sobrescribirá la remota en la siguiente sincronización).
    /// </summary>
    KeepLocal = 0,

    /// <summary>
    /// Conserva la versión remota del atajo (sobrescribe la versión local).
    /// </summary>
    KeepRemote = 1,

    /// <summary>
    /// Conserva ambas versiones: la local permanece intacta y la remota se importa como un nuevo atajo con código alternativo.
    /// </summary>
    DuplicateBoth = 2
}
