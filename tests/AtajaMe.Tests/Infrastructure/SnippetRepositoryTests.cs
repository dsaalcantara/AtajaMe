using AtajaMe.Domain.Entities;
using AtajaMe.Infrastructure.Repositories;

namespace AtajaMe.Tests.Infrastructure;

public sealed class SnippetRepositoryTests : SqliteTestBase
{
    private readonly SnippetRepository _repository;

    public SnippetRepositoryTests()
    {
        _repository = new SnippetRepository(ConnectionFactory);
    }

    [Fact]
    public async Task AddAndGetByIdAsync_PersistsSnippetSuccessfully()
    {
        var snippet = Snippet.Create("saludo", "Saludo General", "Hola, ¿cómo estás?", null, false);

        await _repository.AddAsync(snippet);
        var retrieved = await _repository.GetByIdAsync(snippet.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(snippet.Id, retrieved.Id);
        Assert.Equal("saludo", retrieved.Code);
        Assert.Equal("Saludo General", retrieved.Name);
        Assert.Equal("Hola, ¿cómo estás?", retrieved.Content);
        Assert.False(retrieved.CaseSensitive);
        Assert.True(retrieved.IsActive);
        Assert.False(retrieved.IsFavorite);
        Assert.False(retrieved.IsInTrash);
    }

    [Fact]
    public async Task GetByCodeAsync_ReturnsMatchingSnippetCaseInsensitively()
    {
        var snippet = Snippet.Create("correo", "Mi Correo", "contacto@ejemplo.com");
        await _repository.AddAsync(snippet);

        var retrieved = await _repository.GetByCodeAsync("CORREO");

        Assert.NotNull(retrieved);
        Assert.Equal(snippet.Id, retrieved.Id);
    }

    [Fact]
    public async Task ExistsCodeAsync_IdentifiesDuplicatesAndExcludesCurrentId()
    {
        var snippet = Snippet.Create("codigo1", "Código Uno", "Contenido");
        await _repository.AddAsync(snippet);

        Assert.True(await _repository.ExistsCodeAsync("codigo1"));
        Assert.True(await _repository.ExistsCodeAsync("CODIGO1"));
        Assert.False(await _repository.ExistsCodeAsync("codigo1", snippet.Id));
        Assert.False(await _repository.ExistsCodeAsync("codigo_inexistente"));
    }

    [Fact]
    public async Task UpdateAsync_ModifiesSnippetAttributes()
    {
        var snippet = Snippet.Create("viejo", "Nombre Viejo", "Texto viejo");
        await _repository.AddAsync(snippet);

        snippet.Update("nuevo", "Nombre Nuevo", "Texto nuevo", null, true);
        await _repository.UpdateAsync(snippet);

        var updated = await _repository.GetByIdAsync(snippet.Id);
        Assert.NotNull(updated);
        Assert.Equal("nuevo", updated.Code);
        Assert.Equal("Nombre Nuevo", updated.Name);
        Assert.Equal("Texto nuevo", updated.Content);
        Assert.True(updated.CaseSensitive);
    }

    [Fact]
    public async Task DeletePermanentAsync_RemovesSnippetFromDatabase()
    {
        var snippet = Snippet.Create("temp", "Temporal", "Texto");
        await _repository.AddAsync(snippet);

        await _repository.DeletePermanentAsync(snippet.Id);

        var retrieved = await _repository.GetByIdAsync(snippet.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task SearchAsync_FiltersByCodeNameAndContent()
    {
        var s1 = Snippet.Create("tel", "Teléfono Oficina", "+34 911 00 00 00");
        var s2 = Snippet.Create("firma", "Firma Laboral", "Atentamente,\nEquipo");
        var s3 = Snippet.Create("direccion", "Oficina Central", "Calle Mayor 1");

        await _repository.AddAsync(s1);
        await _repository.AddAsync(s2);
        await _repository.AddAsync(s3);

        var byName = await _repository.SearchAsync("Oficina");
        Assert.Equal(2, byName.Count);

        var byContent = await _repository.SearchAsync("Atentamente");
        Assert.Single(byContent);
        Assert.Equal(s2.Id, byContent[0].Id);

        var byCode = await _repository.SearchAsync("tel");
        Assert.Single(byCode);
    }

    [Fact]
    public async Task GetFavoritesAsync_ReturnsOnlyFavoritesNotInTrash()
    {
        var s1 = Snippet.Create("c1", "Uno", "1");
        var s2 = Snippet.Create("c2", "Dos", "2");
        s2.ToggleFavorite();
        var s3 = Snippet.Create("c3", "Tres", "3");
        s3.ToggleFavorite();
        s3.MoveToTrash();

        await _repository.AddAsync(s1);
        await _repository.AddAsync(s2);
        await _repository.AddAsync(s3);

        var favorites = await _repository.GetFavoritesAsync();
        Assert.Single(favorites);
        Assert.Equal(s2.Id, favorites[0].Id);
    }

    [Fact]
    public async Task PurgeTrashOlderThanAsync_RemovesOnlyOlderTrashedSnippets()
    {
        var s1 = Snippet.Create("t1", "Papelera Antigua", "1");
        s1.MoveToTrash();
        // Simular fecha antigua
        var oldDate = DateTime.UtcNow.AddDays(-35);
        var oldSnippet = new Snippet(s1.Id, s1.Code, s1.Name, s1.Content, null, false, true, false, true, oldDate);

        var s2 = Snippet.Create("t2", "Papelera Reciente", "2");
        s2.MoveToTrash(); // DeletedAt = Now

        await _repository.AddAsync(oldSnippet);
        await _repository.AddAsync(s2);

        await _repository.PurgeTrashOlderThanAsync(DateTime.UtcNow.AddDays(-30));

        var trash = await _repository.GetTrashAsync();
        Assert.Single(trash);
        Assert.Equal(s2.Id, trash[0].Id);
    }
}
