namespace AtajaMe.Application.Expansion;

/// <summary>
/// Proporciona definiciones y utilidades para identificar los delimitadores de activación en AtajaMe.
/// Según la especificación del MVP (RF-04): espacio, tabulador e intro (retorno de carro y salto de línea).
/// </summary>
public static class ExpansionDelimiters
{
    public const char Space = ' ';
    public const char Tab = '\t';
    public const char CarriageReturn = '\r';
    public const char LineFeed = '\n';

    private static readonly HashSet<char> DelimiterSet = new()
    {
        Space,
        Tab,
        CarriageReturn,
        LineFeed
    };

    /// <summary>
    /// Delimitadores soportados por defecto en el MVP.
    /// </summary>
    public static IReadOnlySet<char> SupportedDelimiters => DelimiterSet;

    /// <summary>
    /// Comprueba si el carácter recibido es un delimitador de activación válido.
    /// </summary>
    public static bool IsDelimiter(char c)
    {
        return DelimiterSet.Contains(c);
    }
}
