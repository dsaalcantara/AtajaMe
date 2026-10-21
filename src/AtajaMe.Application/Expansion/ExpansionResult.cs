using AtajaMe.Domain.Entities;

namespace AtajaMe.Application.Expansion;

/// <summary>
/// Estado resultante tras el procesamiento de una tecla o delimitador por el motor de expansión.
/// </summary>
public enum ExpansionStatus
{
    /// <summary>
    /// No se produjo ninguna coincidencia con un atajo activo o no se presionó un delimitador.
    /// </summary>
    NoMatch,

    /// <summary>
    /// Se encontró un atajo activo coincidente y su contenido está listo para ser inyectado.
    /// </summary>
    Success,

    /// <summary>
    /// Se encontró un atajo activo coincidente, pero requiere que el usuario complete variables interactivas en una ventana flotante.
    /// </summary>
    RequiresInteractiveInput
}

/// <summary>
/// Representa el resultado detallado de evaluar una expansión de atajo.
/// </summary>
public sealed class ExpansionResult
{
    public ExpansionStatus Status { get; }
    public bool IsMatch => Status != ExpansionStatus.NoMatch;
    public bool RequiresInput => Status == ExpansionStatus.RequiresInteractiveInput;

    public Snippet? Snippet { get; }
    public string? ExpandedContent { get; }
    public int CharactersToDelete { get; }
    public char? Delimiter { get; }
    public IReadOnlyList<DynamicVariable> PendingInputs { get; }

    private ExpansionResult(
        ExpansionStatus status,
        Snippet? snippet = null,
        string? expandedContent = null,
        int charactersToDelete = 0,
        char? delimiter = null,
        IReadOnlyList<DynamicVariable>? pendingInputs = null)
    {
        Status = status;
        Snippet = snippet;
        ExpandedContent = expandedContent;
        CharactersToDelete = charactersToDelete;
        Delimiter = delimiter;
        PendingInputs = pendingInputs ?? Array.Empty<DynamicVariable>();
    }

    private static readonly ExpansionResult NoMatchInstance = new(ExpansionStatus.NoMatch);

    public static ExpansionResult NoMatch() => NoMatchInstance;

    public static ExpansionResult CreateSuccess(
        Snippet snippet,
        string expandedContent,
        int charactersToDelete,
        char delimiter)
    {
        return new ExpansionResult(
            ExpansionStatus.Success,
            snippet ?? throw new ArgumentNullException(nameof(snippet)),
            expandedContent ?? throw new ArgumentNullException(nameof(expandedContent)),
            charactersToDelete,
            delimiter);
    }

    public static ExpansionResult CreateRequiresInput(
        Snippet snippet,
        string partiallyResolvedContent,
        IReadOnlyList<DynamicVariable> pendingInputs,
        int charactersToDelete,
        char delimiter)
    {
        return new ExpansionResult(
            ExpansionStatus.RequiresInteractiveInput,
            snippet ?? throw new ArgumentNullException(nameof(snippet)),
            partiallyResolvedContent ?? throw new ArgumentNullException(nameof(partiallyResolvedContent)),
            charactersToDelete,
            delimiter,
            pendingInputs ?? throw new ArgumentNullException(nameof(pendingInputs)));
    }
}
