using AtajaMe.Application.Variables;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Repositories;

namespace AtajaMe.Application.Expansion;

/// <summary>
/// Motor central para la detección, validación y expansión de atajos de texto.
/// </summary>
public sealed class SnippetExpansionEngine : ISnippetExpansionEngine, IDisposable
{
    private readonly ISnippetRepository _snippetRepository;
    private readonly IVariableResolutionService _variableService;
    private readonly ICircularKeyBuffer _buffer;
    private readonly bool _ownsBuffer;

    public ICircularKeyBuffer Buffer => _buffer;

    public SnippetExpansionEngine(
        ISnippetRepository snippetRepository,
        IVariableResolutionService variableService,
        ICircularKeyBuffer? buffer = null)
    {
        _snippetRepository = snippetRepository ?? throw new ArgumentNullException(nameof(snippetRepository));
        _variableService = variableService ?? throw new ArgumentNullException(nameof(variableService));

        if (buffer != null)
        {
            _buffer = buffer;
            _ownsBuffer = false;
        }
        else
        {
            _buffer = new CircularKeyBuffer();
            _ownsBuffer = true;
        }
    }

    public async Task<ExpansionResult> ProcessKeyAsync(char key, CancellationToken cancellationToken = default)
    {
        if (key == '\b')
        {
            _buffer.HandleBackspace();
            return ExpansionResult.NoMatch();
        }

        if (ExpansionDelimiters.IsDelimiter(key))
        {
            var bufferedCode = _buffer.GetContent();
            _buffer.Clear(); // Siempre purgar el búfer al llegar a un delimitador (RF-05)

            if (string.IsNullOrEmpty(bufferedCode))
            {
                return ExpansionResult.NoMatch();
            }

            return await TryExpandCodeAsync(bufferedCode, key, cancellationToken);
        }

        _buffer.Append(key);
        return ExpansionResult.NoMatch();
    }

    public async Task<ExpansionResult> TryExpandCodeAsync(
        string code,
        char delimiter,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return ExpansionResult.NoMatch();
        }

        var snippet = await _snippetRepository.GetByCodeAsync(code, cancellationToken);
        if (snippet is null)
        {
            return ExpansionResult.NoMatch();
        }

        // Filtro estricto: no expandir si está en papelera o inactivo
        if (snippet.IsInTrash || !snippet.IsActive)
        {
            return ExpansionResult.NoMatch();
        }

        // Validación de coincidencia de código (sensibilidad a mayúsculas/minúsculas)
        if (!snippet.MatchesCode(code))
        {
            return ExpansionResult.NoMatch();
        }

        var charactersToDelete = code.Length + 1; // Código escrito más el delimitador
        var partiallyResolved = _variableService.ResolveAutomaticVariables(snippet.Content);
        var interactiveVariables = _variableService.ExtractInteractiveVariables(snippet.Content);

        if (interactiveVariables.Count > 0)
        {
            return ExpansionResult.CreateRequiresInput(
                snippet,
                partiallyResolved,
                interactiveVariables,
                charactersToDelete,
                delimiter);
        }

        return ExpansionResult.CreateSuccess(
            snippet,
            partiallyResolved,
            charactersToDelete,
            delimiter);
    }

    public string FinalizeInteractiveExpansion(
        Snippet snippet,
        string partiallyResolvedContent,
        IReadOnlyDictionary<string, string> userInputs)
    {
        ArgumentNullException.ThrowIfNull(snippet);
        ArgumentNullException.ThrowIfNull(partiallyResolvedContent);
        ArgumentNullException.ThrowIfNull(userInputs);

        return _variableService.ResolveInteractiveVariables(partiallyResolvedContent, userInputs);
    }

    public async Task RecordExpansionUsageAsync(Guid snippetId, CancellationToken cancellationToken = default)
    {
        var snippet = await _snippetRepository.GetByIdAsync(snippetId, cancellationToken);
        if (snippet is not null)
        {
            snippet.RecordUsage();
            await _snippetRepository.UpdateAsync(snippet, cancellationToken);
        }
    }

    public void ResetBuffer()
    {
        _buffer.Clear();
    }

    public void Dispose()
    {
        if (_ownsBuffer)
        {
            _buffer.Dispose();
        }
    }
}
