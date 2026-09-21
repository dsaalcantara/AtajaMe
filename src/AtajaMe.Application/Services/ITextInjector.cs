namespace AtajaMe.Application.Services;

/// <summary>
/// Contrato para la inyección de texto en la aplicación de destino activa.
/// Borra el código disparador y su delimitador mediante pulsaciones de retroceso (VK_BACK)
/// y escribe el texto expandido mediante simulación nativa Unicode, respetando el portapapeles (RF-06).
/// </summary>
public interface ITextInjector
{
    /// <summary>
    /// Borra un número determinado de caracteres disparadores e inyecta la cadena de reemplazo.
    /// </summary>
    /// <param name="backspacesToDelete">Cantidad de retrocesos (VK_BACK) a emitir para borrar el código y delimitador.</param>
    /// <param name="textToInject">Texto plano expandido a escribir en el control de destino.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>True si la inyección se ejecutó satisfactoriamente; false en caso contrario.</returns>
    Task<bool> InjectAsync(int backspacesToDelete, string textToInject, CancellationToken cancellationToken = default);
}
