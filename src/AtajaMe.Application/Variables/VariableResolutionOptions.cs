namespace AtajaMe.Application.Variables;

/// <summary>
/// Opciones de configuración de formato para la resolución de variables dinámicas automáticas.
/// </summary>
public sealed class VariableResolutionOptions
{
    /// <summary>
    /// Formato estándar o personalizado para la variable {date}.
    /// Por defecto "d" (fecha corta según la cultura del sistema).
    /// </summary>
    public string DateFormat { get; set; } = "d";

    /// <summary>
    /// Formato estándar o personalizado para la variable {time}.
    /// Por defecto "t" (hora corta según la cultura del sistema).
    /// </summary>
    public string TimeFormat { get; set; } = "t";

    /// <summary>
    /// Formato estándar o personalizado para la variable {datetime}.
    /// Por defecto "g" (fecha corta y hora corta según la cultura del sistema).
    /// </summary>
    public string DateTimeFormat { get; set; } = "g";
}
