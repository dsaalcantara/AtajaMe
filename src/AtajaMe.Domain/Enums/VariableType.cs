namespace AtajaMe.Domain.Enums;

/// <summary>
/// Tipos de variables dinámicas soportadas por el motor de expansión de AtajaMe.
/// </summary>
public enum VariableType
{
    /// <summary>
    /// Fecha actual del sistema ({date}).
    /// </summary>
    SystemDate,

    /// <summary>
    /// Hora actual del sistema ({time}).
    /// </summary>
    SystemTime,

    /// <summary>
    /// Fecha y hora combinada del sistema ({datetime}).
    /// </summary>
    SystemDateTime,

    /// <summary>
    /// Variable interactiva solicitada al usuario mediante ventana emergente ({input:Nombre}).
    /// </summary>
    InteractiveInput,

    /// <summary>
    /// Posicionamiento del cursor tras la expansión ({cursor}).
    /// </summary>
    CursorPosition
}
