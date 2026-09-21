using System.IO;
using AtajaMe.Application.Csv.Parser;
using AtajaMe.Application.Csv.Services;
using AtajaMe.Domain.Entities;
using AtajaMe.Infrastructure.Repositories;
using AtajaMe.Tests.Infrastructure;

namespace AtajaMe.Tests.Csv;

public class CsvExportTests : SqliteTestBase
{
    [Fact]
    public async Task ExportAllAsync_ExportsActiveSnippetsWithFolderHierarchy()
    {
        var snippetRepo = new SnippetRepository(ConnectionFactory);
        var folderRepo = new FolderRepository(ConnectionFactory);
        var service = new CsvImportExportService(snippetRepo, folderRepo);

        // Crear jerarquía de carpetas: Padre -> Hijo
        var parentFolder = Folder.Create("Trabajo");
        await folderRepo.AddAsync(parentFolder);

        var childFolder = Folder.Create("Plantillas", parentFolder.Id);
        await folderRepo.AddAsync(childFolder);

        // Crear snippets
        var s1 = Snippet.Create("!raiz", "En Raíz", "Contenido raíz");
        var s2 = Snippet.Create("!hijo", "En Hijo", "Contenido en plantilla", childFolder.Id);
        var s3 = Snippet.Create("!papelera", "En Papelera", "Contenido papelera");
        s3.MoveToTrash();

        await snippetRepo.AddAsync(s1);
        await snippetRepo.AddAsync(s2);
        await snippetRepo.AddAsync(s3);

        using var writer = new StringWriter();
        await service.ExportAllAsync(writer);
        string csvOutput = writer.ToString();

        using var reader = new StringReader(csvOutput);
        var rows = CsvReaderHelper.Read(reader);

        // Solo 2 activos (el de papelera se omite)
        Assert.Equal(2, rows.Count);

        var rowRaiz = rows.First(r => r.Code == "!raiz");
        Assert.Equal("", rowRaiz.FolderPath);
        Assert.Equal("Contenido raíz", rowRaiz.Content);

        var rowHijo = rows.First(r => r.Code == "!hijo");
        Assert.Equal("Trabajo/Plantillas", rowHijo.FolderPath);
        Assert.Equal("Contenido en plantilla", rowHijo.Content);
    }

    [Fact]
    public async Task ExportFolderAsync_ExportsOnlySpecifiedFolderAndDescendants()
    {
        var snippetRepo = new SnippetRepository(ConnectionFactory);
        var folderRepo = new FolderRepository(ConnectionFactory);
        var service = new CsvImportExportService(snippetRepo, folderRepo);

        var f1 = Folder.Create("Carpeta1");
        await folderRepo.AddAsync(f1);

        var f1Sub = Folder.Create("SubCarpeta", f1.Id);
        await folderRepo.AddAsync(f1Sub);

        var f2 = Folder.Create("Carpeta2");
        await folderRepo.AddAsync(f2);

        var s1 = Snippet.Create("!c1", "C1", "Contenido C1", f1.Id);
        var s1Sub = Snippet.Create("!c1sub", "C1Sub", "Contenido Sub", f1Sub.Id);
        var s2 = Snippet.Create("!c2", "C2", "Contenido C2", f2.Id);

        await snippetRepo.AddAsync(s1);
        await snippetRepo.AddAsync(s1Sub);
        await snippetRepo.AddAsync(s2);

        using var writer = new StringWriter();
        await service.ExportFolderAsync(f1.Id, writer);
        string csvOutput = writer.ToString();

        using var reader = new StringReader(csvOutput);
        var rows = CsvReaderHelper.Read(reader);

        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.Code == "!c1");
        Assert.Contains(rows, r => r.Code == "!c1sub");
        Assert.DoesNotContain(rows, r => r.Code == "!c2");
    }

    [Fact]
    public async Task ExportSnippetAsync_ExportsSingleSnippet()
    {
        var snippetRepo = new SnippetRepository(ConnectionFactory);
        var folderRepo = new FolderRepository(ConnectionFactory);
        var service = new CsvImportExportService(snippetRepo, folderRepo);

        var s = Snippet.Create("!unico", "Unico", "Contenido único");
        await snippetRepo.AddAsync(s);

        using var writer = new StringWriter();
        await service.ExportSnippetAsync(s.Id, writer);
        string csvOutput = writer.ToString();

        using var reader = new StringReader(csvOutput);
        var rows = CsvReaderHelper.Read(reader);

        Assert.Single(rows);
        Assert.Equal("!unico", rows[0].Code);
        Assert.Equal("Contenido único", rows[0].Content);
    }
}
