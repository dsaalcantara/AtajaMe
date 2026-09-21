using AtajaMe.Application.Expansion;

namespace AtajaMe.Application.Services;

/// <summary>
/// Argumentos para el evento de solicitud de variables interactivas en un atajo.
/// </summary>
public sealed class InteractiveInputRequestedEventArgs : EventArgs
{
    public ExpansionResult Result { get; }
    public IntPtr WindowHandle { get; }

    public InteractiveInputRequestedEventArgs(ExpansionResult result, IntPtr windowHandle)
    {
        Result = result ?? throw new ArgumentNullException(nameof(result));
        WindowHandle = windowHandle;
    }
}

/// <summary>
/// Contrato del servicio orquestador que consume los eventos del hook de teclado,
/// valida seguridad, consulta exclusiones e interactúa con el motor de expansión y el inyector.
/// </summary>
public interface IKeyboardExpansionOrchestrator : IDisposable
{
    /// <summary>
    /// Evento disparado cuando un atajo detectado requiere variables interactivas ingresadas por el usuario.
    /// </summary>
    event EventHandler<InteractiveInputRequestedEventArgs>? InteractiveInputRequested;

    /// <summary>
    /// Indica si el orquestador está ejecutándose en segundo plano.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Indica si la captura de atajos está temporalmente pausada (ej. por el usuario en la bandeja).
    /// </summary>
    bool IsPaused { get; set; }

    /// <summary>
    /// Inicia el bucle de procesamiento en segundo plano del orquestador.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Detiene el bucle de procesamiento del orquestador.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Completa la inyección de un atajo que requería variables interactivas tras ser completadas por la interfaz.
    /// </summary>
    Task<bool> CompleteInteractiveExpansionAsync(
        ExpansionResult pendingResult,
        IReadOnlyDictionary<string, string> userValues,
        IntPtr expectedWindowHandle,
        CancellationToken cancellationToken = default);
}
