namespace AtajaMe.Application.Services;

/// <summary>
/// Representa un evento de teclado de bajo nivel capturado por el hook del sistema operativo.
/// Es inmutable y no almacena información privada en almacenamiento persistente.
/// </summary>
public readonly struct RawKeyEvent
{
    /// <summary>
    /// Código de tecla virtual de Windows (ej. VK_A, VK_SPACE, VK_RETURN, etc.).
    /// </summary>
    public int VirtualKeyCode { get; }

    /// <summary>
    /// Código de escaneo de hardware de la tecla.
    /// </summary>
    public int ScanCode { get; }

    /// <summary>
    /// Banderas asociadas al evento (ej. tecla extendida, pulsación previa, etc.).
    /// </summary>
    public uint Flags { get; }

    /// <summary>
    /// Marca temporal en milisegundos del sistema al producirse el evento.
    /// </summary>
    public uint Timestamp { get; }

    /// <summary>
    /// Carácter interpretado si corresponde a un carácter imprimible o de control (ej. '\b', ' ', 'a').
    /// Si no puede convertirse directamente o es una tecla de función/navegación, es null.
    /// </summary>
    public char? Character { get; }

    /// <summary>
    /// Indica si el evento corresponde a una pulsación (KeyDown) frente a una liberación (KeyUp).
    /// </summary>
    public bool IsKeyDown { get; }

    public RawKeyEvent(
        int virtualKeyCode,
        int scanCode,
        uint flags,
        uint timestamp,
        char? character = null,
        bool isKeyDown = true)
    {
        VirtualKeyCode = virtualKeyCode;
        ScanCode = scanCode;
        Flags = flags;
        Timestamp = timestamp;
        Character = character;
        IsKeyDown = isKeyDown;
    }

    public override string ToString() =>
        $"RawKeyEvent: VK={VirtualKeyCode}, Scan={ScanCode}, Char='{Character}', Down={IsKeyDown}";
}
