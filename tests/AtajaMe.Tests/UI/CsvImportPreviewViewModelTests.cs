using AtajaMe.Application.Csv.DTOs;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Enums;
using AtajaMe.UI.ViewModels;

namespace AtajaMe.Tests.UI;

public sealed class CsvImportPreviewViewModelTests
{
    [Fact]
    public void Constructor_ExtractsCollisionsAndErrors()
    {
        var existing = Snippet.Create("c1", "Atajo Existente", "Contenido");
        var item1 = new CsvImportItemPreview(1, new CsvSnippetRow("c1", "Nombre", "Contenido"), true, existing);
        var item2 = new CsvImportItemPreview(2, new CsvSnippetRow("", "Sin codigo", "Contenido"), false, null, "Código requerido");

        var preview = new CsvImportPreview(
            TotalRows: 2,
            ValidRows: 1,
            ConflictCount: 1,
            InvalidRows: 1,
            Items: new[] { item1, item2 });

        var vm = new CsvImportPreviewViewModel(preview);

        Assert.Equal(2, vm.TotalRows);
        Assert.Equal(1, vm.ValidSnippetsCount);
        Assert.Equal(1, vm.CollisionsCount);
        Assert.Equal(1, vm.ErrorsCount);
        Assert.True(vm.HasCollisions);
        Assert.True(vm.HasErrors);
        Assert.Single(vm.Collisions);
        Assert.Single(vm.Errors);
    }

    [Fact]
    public void DefaultResolution_ChangesUpdatesAllItems()
    {
        var existing = Snippet.Create("c1", "Existente", "Contenido");
        var item = new CsvImportItemPreview(1, new CsvSnippetRow("c1", "Nombre", "Contenido"), true, existing);
        var preview = new CsvImportPreview(1, 1, 1, 0, new[] { item });

        var vm = new CsvImportPreviewViewModel(preview);
        vm.DefaultResolution = CsvConflictResolution.Overwrite;

        Assert.Equal(CsvConflictResolution.Overwrite, vm.Collisions[0].Resolution);
        var overrides = vm.GetResolutionOverrides();
        Assert.Equal(CsvConflictResolution.Overwrite, overrides["c1"]);
    }

    [Fact]
    public void ConfirmCommand_SetsIsConfirmedTrue_AndRaisesRequestClose()
    {
        var preview = new CsvImportPreview(0, 0, 0, 0, Array.Empty<CsvImportItemPreview>());
        var vm = new CsvImportPreviewViewModel(preview);

        bool closed = false;
        vm.RequestClose += () => closed = true;

        vm.ConfirmCommand.Execute(null);

        Assert.True(vm.IsConfirmed);
        Assert.True(closed);
    }

    [Fact]
    public void Constructor_PopulatesValidSnippetsAndSelectsAppropriateTab()
    {
        var item1 = new CsvImportItemPreview(1, new CsvSnippetRow("c1", "Nombre 1", "Contenido 1", "00 Comunes"), false, null);
        var item2 = new CsvImportItemPreview(2, new CsvSnippetRow("c2", "Nombre 2", "Contenido 2", "00 Comunes/Saludos"), false, null);

        var preview = new CsvImportPreview(2, 2, 0, 0, new[] { item1, item2 });
        var vm = new CsvImportPreviewViewModel(preview);

        Assert.Equal(2, vm.ValidSnippets.Count);
        Assert.Empty(vm.Collisions);
        Assert.Empty(vm.Errors);
        Assert.Equal(0, vm.SelectedTabIndex); // Pestaña de atajos válidos seleccionada por defecto
        Assert.True(vm.CanImport);
        Assert.Equal("00 Comunes", vm.ValidSnippets[0].FolderPath);
        Assert.Equal("00 Comunes/Saludos", vm.ValidSnippets[1].FolderPath);
    }
}
