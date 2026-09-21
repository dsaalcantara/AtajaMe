using AtajaMe.Application.Expansion;
using AtajaMe.Application.Variables;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Repositories;

namespace AtajaMe.Tests.Expansion;

public class SnippetExpansionEngineTests
{
    private readonly FakeSnippetRepository _repository = new();
    private readonly VariableResolutionService _variableService = new();

    private SnippetExpansionEngine CreateEngine(ICircularKeyBuffer? buffer = null)
    {
        return new SnippetExpansionEngine(_repository, _variableService, buffer);
    }

    [Fact]
    public async Task ProcessKeyAsync_NonDelimiter_AccumulatesInBufferAndReturnsNoMatch()
    {
        using var engine = CreateEngine();

        var r1 = await engine.ProcessKeyAsync('h');
        var r2 = await engine.ProcessKeyAsync('i');

        Assert.Equal(ExpansionStatus.NoMatch, r1.Status);
        Assert.Equal(ExpansionStatus.NoMatch, r2.Status);
        Assert.Equal("hi", engine.Buffer.GetContent());
    }

    [Fact]
    public async Task ProcessKeyAsync_Backspace_RemovesLastCharacter()
    {
        using var engine = CreateEngine();

        await engine.ProcessKeyAsync('b');
        await engine.ProcessKeyAsync('x');
        await engine.ProcessKeyAsync('\b');
        await engine.ProcessKeyAsync('d');

        Assert.Equal("bd", engine.Buffer.GetContent());
    }

    [Theory]
    [InlineData(' ')]
    [InlineData('\t')]
    [InlineData('\r')]
    [InlineData('\n')]
    public async Task ProcessKeyAsync_ActiveSnippet_ExpandsOnAllDelimiters(char delimiter)
    {
        using var engine = CreateEngine();
        var snippet = Snippet.Create("bd", "Buenos días", "¡Buenos días a todos!");
        await _repository.AddAsync(snippet);

        foreach (var c in "bd")
        {
            await engine.ProcessKeyAsync(c);
        }

        var result = await engine.ProcessKeyAsync(delimiter);

        Assert.Equal(ExpansionStatus.Success, result.Status);
        Assert.True(result.IsMatch);
        Assert.NotNull(result.Snippet);
        Assert.Equal("¡Buenos días a todos!", result.ExpandedContent);
        Assert.Equal(3, result.CharactersToDelete); // "bd".Length (2) + 1 = 3
        Assert.Equal(delimiter, result.Delimiter);
        Assert.True(engine.Buffer.IsEmpty, "El búfer debe purgarse tras la expansión.");
    }

    [Fact]
    public async Task ProcessKeyAsync_NoMatchingSnippet_ClearsBuffer()
    {
        using var engine = CreateEngine();

        foreach (var c in "desconocido")
        {
            await engine.ProcessKeyAsync(c);
        }

        var result = await engine.ProcessKeyAsync(' ');

        Assert.Equal(ExpansionStatus.NoMatch, result.Status);
        Assert.False(result.IsMatch);
        Assert.True(engine.Buffer.IsEmpty, "El búfer debe purgarse tras un delimitador sin coincidencia (RF-05).");
    }

    [Fact]
    public async Task ProcessKeyAsync_CaseSensitive_MatchesOnlyExactCase()
    {
        using var engine = CreateEngine();
        var snippet = Snippet.Create("SQL", "SQL Upper", "SELECT * FROM Table", caseSensitive: true);
        await _repository.AddAsync(snippet);

        // Intento 1: minúsculas "sql"
        foreach (var c in "sql")
        {
            await engine.ProcessKeyAsync(c);
        }
        var resultLower = await engine.ProcessKeyAsync(' ');
        Assert.Equal(ExpansionStatus.NoMatch, resultLower.Status);

        // Intento 2: mayúsculas exactas "SQL"
        foreach (var c in "SQL")
        {
            await engine.ProcessKeyAsync(c);
        }
        var resultExact = await engine.ProcessKeyAsync(' ');
        Assert.Equal(ExpansionStatus.Success, resultExact.Status);
        Assert.Equal("SELECT * FROM Table", resultExact.ExpandedContent);
    }

    [Fact]
    public async Task ProcessKeyAsync_CaseInsensitive_MatchesAnyCase()
    {
        using var engine = CreateEngine();
        var snippet = Snippet.Create("saludo", "Saludo", "Hola amigo", caseSensitive: false);
        await _repository.AddAsync(snippet);

        foreach (var c in "SaLuDo")
        {
            await engine.ProcessKeyAsync(c);
        }

        var result = await engine.ProcessKeyAsync(' ');

        Assert.Equal(ExpansionStatus.Success, result.Status);
        Assert.Equal("Hola amigo", result.ExpandedContent);
    }

    [Fact]
    public async Task ProcessKeyAsync_InactiveSnippet_DoesNotExpand()
    {
        using var engine = CreateEngine();
        var snippet = Snippet.Create("inactivo", "Inactivo", "No expandir");
        snippet.ToggleActive(); // IsActive = false
        await _repository.AddAsync(snippet);

        foreach (var c in "inactivo")
        {
            await engine.ProcessKeyAsync(c);
        }

        var result = await engine.ProcessKeyAsync(' ');

        Assert.Equal(ExpansionStatus.NoMatch, result.Status);
    }

    [Fact]
    public async Task ProcessKeyAsync_TrashSnippet_DoesNotExpand()
    {
        using var engine = CreateEngine();
        var snippet = Snippet.Create("papelera", "En papelera", "No expandir");
        snippet.MoveToTrash(); // IsInTrash = true
        await _repository.AddAsync(snippet);

        foreach (var c in "papelera")
        {
            await engine.ProcessKeyAsync(c);
        }

        var result = await engine.ProcessKeyAsync(' ');

        Assert.Equal(ExpansionStatus.NoMatch, result.Status);
    }

    [Fact]
    public async Task ProcessKeyAsync_AutomaticVariables_AreResolvedInExpandedContent()
    {
        using var engine = CreateEngine();
        var snippet = Snippet.Create("hoy", "Fecha de hoy", "Fecha de emisión: {date}");
        await _repository.AddAsync(snippet);

        foreach (var c in "hoy")
        {
            await engine.ProcessKeyAsync(c);
        }

        var result = await engine.ProcessKeyAsync(' ');

        Assert.Equal(ExpansionStatus.Success, result.Status);
        Assert.DoesNotContain("{date}", result.ExpandedContent);
        Assert.Contains(DateTime.Now.ToString("d"), result.ExpandedContent);
    }

    [Fact]
    public async Task ProcessKeyAsync_InteractiveVariables_ReturnsRequiresInteractiveInput()
    {
        using var engine = CreateEngine();
        var snippet = Snippet.Create("saludar", "Saludo interactivo", "Hola {input:nombre|amigo}, hoy es {date}.");
        await _repository.AddAsync(snippet);

        foreach (var c in "saludar")
        {
            await engine.ProcessKeyAsync(c);
        }

        var result = await engine.ProcessKeyAsync(' ');

        Assert.Equal(ExpansionStatus.RequiresInteractiveInput, result.Status);
        Assert.True(result.RequiresInput);
        Assert.Single(result.PendingInputs);
        Assert.Equal("nombre", result.PendingInputs[0].Name);
        Assert.Equal("amigo", result.PendingInputs[0].DefaultValue);
        Assert.DoesNotContain("{date}", result.ExpandedContent); // Las automáticas ya están resueltas

        // Finalizar con entrada del usuario
        var userInputs = new Dictionary<string, string> { ["nombre"] = "Roberto" };
        var finalContent = engine.FinalizeInteractiveExpansion(snippet, result.ExpandedContent!, userInputs);

        Assert.StartsWith("Hola Roberto, hoy es ", finalContent);
    }

    [Fact]
    public async Task RecordExpansionUsageAsync_IncrementsUsageCountAndUpdatesLastUsedAt()
    {
        using var engine = CreateEngine();
        var snippet = Snippet.Create("uso", "Prueba de uso", "Contenido");
        await _repository.AddAsync(snippet);

        Assert.Equal(0, snippet.UsageCount);
        Assert.Null(snippet.LastUsedAt);

        await engine.RecordExpansionUsageAsync(snippet.Id);

        var updated = await _repository.GetByIdAsync(snippet.Id);
        Assert.NotNull(updated);
        Assert.Equal(1, updated.UsageCount);
        Assert.NotNull(updated.LastUsedAt);
    }

    [Fact]
    public void ResetBuffer_EmptiesBuffer()
    {
        using var engine = CreateEngine();
        engine.Buffer.Append('a');
        engine.Buffer.Append('b');

        Assert.False(engine.Buffer.IsEmpty);

        engine.ResetBuffer();

        Assert.True(engine.Buffer.IsEmpty);
    }

    // Repositorio en memoria simplificado para pruebas unitarias directas
    private sealed class FakeSnippetRepository : ISnippetRepository
    {
        private readonly List<Snippet> _snippets = new();

        public Task AddAsync(Snippet snippet, CancellationToken cancellationToken = default)
        {
            _snippets.Add(snippet);
            return Task.CompletedTask;
        }

        public Task<Snippet?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            // Simula comportamiento de SQLite con NOCASE e IsInTrash = 0
            var match = _snippets.FirstOrDefault(s =>
                !s.IsInTrash && string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(match);
        }

        public Task<Snippet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var match = _snippets.FirstOrDefault(s => s.Id == id);
            return Task.FromResult(match);
        }

        public Task UpdateAsync(Snippet snippet, CancellationToken cancellationToken = default)
        {
            var idx = _snippets.FindIndex(s => s.Id == snippet.Id);
            if (idx >= 0)
            {
                _snippets[idx] = snippet;
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Snippet>> GetAllActiveAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Snippet>>(_snippets.Where(s => s.IsActive && !s.IsInTrash).ToList());

        public Task<IReadOnlyList<Snippet>> GetByFolderIdAsync(Guid? folderId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Snippet>>(_snippets.Where(s => s.FolderId == folderId).ToList());

        public Task<IReadOnlyList<Snippet>> GetFavoritesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Snippet>>(_snippets.Where(s => s.IsFavorite && !s.IsInTrash).ToList());

        public Task<IReadOnlyList<Snippet>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Snippet>>(_snippets.OrderByDescending(s => s.LastUsedAt).Take(limit).ToList());

        public Task<IReadOnlyList<Snippet>> GetTrashAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Snippet>>(_snippets.Where(s => s.IsInTrash).ToList());

        public Task<IReadOnlyList<Snippet>> SearchAsync(string query, Guid? folderId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Snippet>>(_snippets);

        public Task DeletePermanentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _snippets.RemoveAll(s => s.Id == id);
            return Task.CompletedTask;
        }

        public Task PurgeTrashOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
        {
            _snippets.RemoveAll(s => s.IsInTrash && s.DeletedAt.HasValue && s.DeletedAt.Value < cutoffDate);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsCodeAsync(string code, Guid? excludeSnippetId = null, CancellationToken cancellationToken = default)
        {
            var exists = _snippets.Any(s =>
                string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase) &&
                (!excludeSnippetId.HasValue || s.Id != excludeSnippetId.Value));
            return Task.FromResult(exists);
        }
    }
}
