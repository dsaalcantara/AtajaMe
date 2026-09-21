namespace AtajaMe.Application.Services;

/// <summary>
/// Contrato para el seguimiento del foco y la ventana activa en el sistema operativo.
/// Permite validar si el foco ha cambiado durante un proceso de expansión (RF-10)
/// y obtener el proceso activo para comprobar exclusiones (RF-07).
/// </summary>
public interface IFocusTracker
{
    /// <summary>
    /// Obtiene el manejador nativo (HWND) de la ventana actualmente en primer plano.
    /// </summary>
    IntPtr GetActiveWindowHandle();

    /// <summary>
    /// Obtiene el nombre del proceso ejecutable de la ventana actualmente en primer plano (ej. "notepad.exe", "chrome.exe").
    /// Si no puede determinarse, devuelve null o string vacío.
    /// </summary>
    string? GetActiveProcessName();

    /// <summary>
    /// Comprueba si la ventana activa ha cambiado respecto al manejador de ventana inicial.
    /// </summary>
    /// <param name="originalWindowHandle">Manejador de la ventana capturada al inicio.</param>
    /// <returns>True si el foco/ventana ha cambiado o ya no es válido; false si se mantiene igual.</returns>
    bool HasFocusChanged(IntPtr originalWindowHandle);
}
