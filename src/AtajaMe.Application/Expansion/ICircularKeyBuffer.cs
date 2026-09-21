namespace AtajaMe.Application.Expansion;

/// <summary>
/// Contrato del búfer efímero de pulsaciones de teclado en memoria.
/// </summary>
public interface ICircularKeyBuffer : IDisposable
{
    /// <summary>
    /// Capacidad máxima de caracteres en memoria.
    /// </summary>
    int MaxCapacity { get; }

    /// <summary>
    /// Número actual de caracteres en el búfer.
    /// </summary>
    int Length { get; }

    /// <summary>
    /// Indica si el búfer está vacío.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Tiempo límite de inactividad antes de purgar el búfer.
    /// </summary>
    TimeSpan IdleTimeout { get; }

    /// <summary>
    /// Se dispara cuando el búfer se purga automáticamente por inactividad o de forma explícita.
    /// </summary>
    event EventHandler? BufferPurged;

    /// <summary>
    /// Añade un carácter al búfer efímero y reinicia el temporizador de inactividad.
    /// Si se alcanza la capacidad máxima, se descarta el carácter más antiguo.
    /// </summary>
    void Append(char c);

    /// <summary>
    /// Procesa una pulsación de retroceso (Backspace), eliminando el carácter más reciente.
    /// </summary>
    /// <returns>True si se eliminó un carácter; false si el búfer ya estaba vacío.</returns>
    bool HandleBackspace();

    /// <summary>
    /// Obtiene el contenido textual actual del búfer.
    /// </summary>
    string GetContent();

    /// <summary>
    /// Limpia de inmediato todo el contenido del búfer y cancela el temporizador de inactividad.
    /// </summary>
    void Clear();
}
