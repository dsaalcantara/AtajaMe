using System.IO;
using AtajaMe.Application.Csv.Services;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Enums;
using AtajaMe.Infrastructure.Repositories;
using AtajaMe.Tests.Infrastructure;

namespace AtajaMe.Tests.Csv;

public class CsvImportTests : SqliteTestBase
{
    [Fact]
    public async Task AnalyzeImportAsync_DetectsValidAndConflictingRows()
    {
        var snippetRepo = new SnippetRepository(ConnectionFactory);
        var folderRepo = new FolderRepository(ConnectionFactory);
        var service = new CsvImportExportService(snippetRepo, folderRepo);

        // Insertar snippet existente
        await snippetRepo.AddAsync(Snippet.Create("!existente", "Existente", "Texto original"));

        string csv = "Code,Name,Content,FolderPath\r\n" +
                     "!existente,Existente Remoto,Texto modificado,Trabajo\r\n" +
                     "!nuevo,Nuevo,Texto nuevo,Personal\r\n" +
                     ",Invalido,Sin codigo,";

        using var reader = new StringReader(csv);
        var preview = await service.AnalyzeImportAsync(reader);

        Assert.Equal(3, preview.TotalRows);
        Assert.Equal(2, preview.ValidRows);
        Assert.Equal(1, preview.ConflictCount);
        Assert.Equal(1, preview.InvalidRows);

        var itemConflicto = preview.Items.First(i => i.Row.Code == "!existente");
        Assert.True(itemConflicto.HasConflict);
        Assert.NotNull(itemConflicto.ExistingSnippet);

        var itemNuevo = preview.Items.First(i => i.Row.Code == "!nuevo");
        Assert.False(itemNuevo.HasConflict);

        var itemInvalido = preview.Items.First(i => i.RowIndex == 3);
        Assert.False(itemInvalido.IsValid);
        Assert.Contains("obligatorio", itemInvalido.ValidationErrorMessage!);
    }

    [Fact]
    public async Task ExecuteImportAsync_CreatesFoldersAndSnippetsCorrectly()
    {
        var snippetRepo = new SnippetRepository(ConnectionFactory);
        var folderRepo = new FolderRepository(ConnectionFactory);
        var service = new CsvImportExportService(snippetRepo, folderRepo);

        string csv = "Code,Name,Content,FolderPath,CaseSensitive,IsActive,IsFavorite,SortOrder\r\n" +
                     "!mail,Email,correo@test.com,Trabajo/Comercial,true,true,true,5\r\n" +
                     "!web,Web,https://atajame.app,,false,true,false,0";

        using var reader = new StringReader(csv);
        var result = await service.ExecuteImportAsync(reader);

        Assert.Equal(2, result.TotalProcessed);
        Assert.Equal(2, result.ImportedCount);
        Assert.False(result.HasErrors);

        // Verificar creación de carpetas Trabajo -> Comercial
        var folders = await folderRepo.GetAllAsync();
        var trabajo = folders.FirstOrDefault(f => f.Name == "Trabajo" && f.ParentId == null);
        Assert.NotNull(trabajo);

        var comercial = folders.FirstOrDefault(f => f.Name == "Comercial" && f.ParentId == trabajo.Id);
        Assert.NotNull(comercial);

        // Verificar atajos
        var snippetMail = await snippetRepo.GetByCodeAsync("!mail");
        Assert.NotNull(snippetMail);
        Assert.Equal("correo@test.com", snippetMail.Content);
        Assert.Equal(comercial.Id, snippetMail.FolderId);
        Assert.True(snippetMail.CaseSensitive);
        Assert.True(snippetMail.IsActive);
        Assert.True(snippetMail.IsFavorite);
    }

    [Fact]
    public async Task ExecuteImportAsync_WithSkipConflictResolution_PreservesExisting()
    {
        var snippetRepo = new SnippetRepository(ConnectionFactory);
        var folderRepo = new FolderRepository(ConnectionFactory);
        var service = new CsvImportExportService(snippetRepo, folderRepo);

        await snippetRepo.AddAsync(Snippet.Create("!dup", "Local", "Texto local original"));

        string csv = "Code,Name,Content\r\n!dup,Remoto,Texto remoto modificado";

        using var reader = new StringReader(csv);
        var result = await service.ExecuteImportAsync(reader, defaultResolution: CsvConflictResolution.Skip);

        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.SkippedCount);

        var current = await snippetRepo.GetByCodeAsync("!dup");
        Assert.NotNull(current);
        Assert.Equal("Local", current.Name);
        Assert.Equal("Texto local original", current.Content);
    }

    [Fact]
    public async Task ExecuteImportAsync_WithOverwriteConflictResolution_UpdatesExisting()
    {
        var snippetRepo = new SnippetRepository(ConnectionFactory);
        var folderRepo = new FolderRepository(ConnectionFactory);
        var service = new CsvImportExportService(snippetRepo, folderRepo);

        await snippetRepo.AddAsync(Snippet.Create("!dup", "Local", "Texto local original"));

        string csv = "Code,Name,Content\r\n!dup,Sobrescrito,Nuevo contenido importado";

        using var reader = new StringReader(csv);
        var result = await service.ExecuteImportAsync(reader, defaultResolution: CsvConflictResolution.Overwrite);

        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(1, result.OverwrittenCount);

        var current = await snippetRepo.GetByCodeAsync("!dup");
        Assert.NotNull(current);
        Assert.Equal("Sobrescrito", current.Name);
        Assert.Equal("Nuevo contenido importado", current.Content);
    }

    [Fact]
    public async Task ExecuteImportAsync_WithDuplicateWithSuffix_CreatesNewSnippet()
    {
        var snippetRepo = new SnippetRepository(ConnectionFactory);
        var folderRepo = new FolderRepository(ConnectionFactory);
        var service = new CsvImportExportService(snippetRepo, folderRepo);

        await snippetRepo.AddAsync(Snippet.Create("!dup", "Local", "Texto local original"));

        string csv = "Code,Name,Content\r\n!dup,Remoto,Texto remoto adicional";

        using var reader = new StringReader(csv);
        var result = await service.ExecuteImportAsync(reader, defaultResolution: CsvConflictResolution.DuplicateWithSuffix);

        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(1, result.DuplicatedCount);

        var original = await snippetRepo.GetByCodeAsync("!dup");
        Assert.NotNull(original);
        Assert.Equal("Texto local original", original.Content);

        var duplicate = await snippetRepo.GetByCodeAsync("!dup_1");
        Assert.NotNull(duplicate);
        Assert.Equal("Texto remoto adicional", duplicate.Content);
    }

    [Fact]
    public async Task ExecuteImportAsync_WithAccentsAndQuotes_ImportsSuccessfully()
    {
        var snippetRepo = new SnippetRepository(ConnectionFactory);
        var folderRepo = new FolderRepository(ConnectionFactory);
        var service = new CsvImportExportService(snippetRepo, folderRepo);

        string csv = @"Code,Name,Content,FolderPath,CaseSensitive,IsActive,IsFavorite,SortOrder
adj_,""Adjunto envío los datos solicitados."",""Adjunto envío los datos solicitados."",""00 Comunes"",false,true,false,1
re__,""Referencia a email para seguimiento de tareas"","" (ref. correo """" """")"",""00 Comunes"",false,true,false,13
h__,Buenos días/tardes/noches,""Buenos días,"",""00 Comunes/Saludos"",false,true,false,17";

        using var reader = new StringReader(csv);
        var preview = await service.AnalyzeImportAsync(reader);

        Assert.Equal(3, preview.TotalRows);
        Assert.Equal(3, preview.ValidRows);
        Assert.Equal(0, preview.InvalidRows);

        using var execReader = new StringReader(csv);
        var result = await service.ExecuteImportAsync(execReader);

        Assert.Equal(3, result.ImportedCount);
        Assert.Empty(result.Errors);

        var s1 = await snippetRepo.GetByCodeAsync("adj_");
        Assert.NotNull(s1);
        Assert.Equal("Adjunto envío los datos solicitados.", s1.Content);

        var s2 = await snippetRepo.GetByCodeAsync("re__");
        Assert.NotNull(s2);
        Assert.Equal(" (ref. correo \" \")", s2.Content);

        var s3 = await snippetRepo.GetByCodeAsync("h__");
        Assert.NotNull(s3);
        Assert.Equal("Buenos días,", s3.Content);

        var folders = (await folderRepo.GetAllAsync()).ToList();
        Assert.Contains(folders, f => f.Name == "00 Comunes");
        Assert.Contains(folders, f => f.Name == "Saludos");
    }
}
