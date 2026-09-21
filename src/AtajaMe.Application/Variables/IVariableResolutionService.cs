using System.Globalization;
using AtajaMe.Domain.Entities;

namespace AtajaMe.Application.Variables;

/// <summary>
/// Contrato para el servicio de resolución y sustitución de variables dinámicas automáticas e interactivas.
/// </summary>
public interface IVariableResolutionService
{
    /// <summary>
    /// Determina si el texto contiene alguna variable dinámica (automática o interactiva).
    /// </summary>
    bool HasVariables(string? content);

    /// <summary>
    /// Determina si el texto contiene variables interactivas pendientes que requieran la intervención del usuario.
    /// </summary>
    bool HasInteractiveVariables(string? content);

    /// <summary>
    /// Extrae la lista de todas las variables dinámicas presentes en el contenido.
    /// </summary>
    IReadOnlyList<DynamicVariable> ExtractVariables(string? content);

    /// <summary>
    /// Extrae exclusivamente las variables interactivas que solicitan datos al usuario ({input:nombre}).
    /// </summary>
    IReadOnlyList<DynamicVariable> ExtractInteractiveVariables(string? content);

    /// <summary>
    /// Sustituye las variables automáticas de sistema ({date}, {time}, {datetime}) en el contenido.
    /// </summary>
    string ResolveAutomaticVariables(string? content, DateTime? referenceTime = null, CultureInfo? culture = null);

    /// <summary>
    /// Sustituye las variables interactivas ({input:nombre}) utilizando los valores provistos por el usuario.
    /// Si un campo no tiene valor pero define valor por defecto ({input:nombre|defecto}), se utiliza este último.
    /// </summary>
    string ResolveInteractiveVariables(string? content, IReadOnlyDictionary<string, string>? userInputs);

    /// <summary>
    /// Resuelve de forma completa tanto las variables automáticas como las interactivas provistas.
    /// </summary>
    string ResolveAll(
        string? content,
        IReadOnlyDictionary<string, string>? userInputs = null,
        DateTime? referenceTime = null,
        CultureInfo? culture = null);
}
