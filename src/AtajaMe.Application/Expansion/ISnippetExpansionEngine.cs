using AtajaMe.Domain.Entities;

namespace AtajaMe.Application.Expansion;

/// <summary>
/// Contrato del motor central de expansión de atajos de AtajaMe.
/// Orquesta la captura de teclas en el búfer efímero, la detección de delimitadores,
/// el filtrado de atajos según sus reglas y la resolución de variables.
/// </summary>
public interface ISnippetExpansionEngine
{
    /// <summary>
    /// Acceso al búfer efímero de teclas.
    /// </summary>
    ICircularKeyBuffer Buffer { get; }

    /// <summary>
    /// Procesa una pulsación de tecla recibida. Si la tecla es un delimitador,
    /// evalúa el contenido actual del búfer y determina si corresponde una expansión.
    /// </summary>
    Task<ExpansionResult> ProcessKeyAsync(char key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Intenta expandir un código concreto asociado a un delimitador específico.
    /// </summary>
    Task<ExpansionResult> TryExpandCodeAsync(string code, char delimiter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finaliza la resolución de un atajo interactivo tras recibir los valores ingresados por el usuario.
    /// </summary>
    string FinalizeInteractiveExpansion(
        Snippet snippet,
        string partiallyResolvedContent,
        IReadOnlyDictionary<string, string> userInputs);

    /// <summary>
    /// Registra el uso del atajo expandido (incrementa contador de uso y actualiza LastUsedAt).
    /// </summary>
    Task RecordExpansionUsageAsync(Guid snippetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Limpia de inmediato el contenido del búfer efímero (útil ante cambio de foco o bloqueo de seguridad).
    /// </summary>
    void ResetBuffer();
}
