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
    CursorPosition,

    /// <summary>
    /// Número de semana del calendario ({cw}, {semana}, {week}).
    /// </summary>
    CalendarWeek,

    /// <summary>
    /// Mes del calendario ({mes}, {month}).
    /// </summary>
    Month,

    /// <summary>
    /// Día del mes o de la semana ({dia}, {day}).
    /// </summary>
    Day,

    /// <summary>
    /// Año del calendario ({año}, {ano}, {year}).
    /// </summary>
    Year,

    /// <summary>
    /// Trimestre del año ({trimestre}, {quarter}, {q}).
    /// </summary>
    Quarter,

    /// <summary>
    /// Cuatrimestre del año ({cuatrimestre}, {fourmonth}, {c}).
    /// </summary>
    FourMonth
}
