using AtajaMe.Domain.Repositories;

namespace AtajaMe.Application.Snippets;

/// <summary>
/// Caso de uso para la purga definitiva de atajos en papelera (inmediata o por política de retención).
/// </summary>
public sealed class PurgeTrashUseCase
{
    private readonly ISnippetRepository _snippetRepository;

    public PurgeTrashUseCase(ISnippetRepository snippetRepository)
    {
        _snippetRepository = snippetRepository;
    }

    /// <summary>
    /// Elimina definitivamente los atajos que lleven en papelera desde antes de la fecha límite especificada.
    /// </summary>
    public async Task PurgeOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        await _snippetRepository.PurgeTrashOlderThanAsync(cutoffDate, cancellationToken);
    }

    /// <summary>
    /// Vacía por completo la papelera eliminando permanentemente todos sus elementos.
    /// </summary>
    public async Task PurgeAllAsync(CancellationToken cancellationToken = default)
    {
        // Purgar todo lo que esté en papelera con fecha menor o igual al instante actual
        await _snippetRepository.PurgeTrashOlderThanAsync(DateTime.UtcNow.AddMinutes(1), cancellationToken);
    }
}
