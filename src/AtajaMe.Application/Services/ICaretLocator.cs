namespace AtajaMe.Application.Services;

/// <summary>
/// Provee las coordenadas de pantalla para posicionar ventanas emergentes cerca del cursor de texto o ratón (RF-12).
/// </summary>
public interface ICaretLocator
{
    /// <summary>
    /// Obtiene las coordenadas de pantalla (X, Y) óptimas para mostrar la ventana emergente contextual.
    /// Intenta consultar el cursor de texto (caret) de la ventana activa; si no está disponible, recurre a la posición del ratón.
    /// </summary>
    (int X, int Y) GetCaretOrCursorPosition(IntPtr windowHandle);
}
