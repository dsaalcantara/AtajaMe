using AtajaMe.Application.DTOs;
using AtajaMe.UI.ViewModels;

namespace AtajaMe.Tests.UI;

public sealed class SnippetEditorViewModelTests
{
    [Fact]
    public void PrepareNewSnippet_InitializesCleanState()
    {
        var vm = new SnippetEditorViewModel();
        var folderId = Guid.NewGuid();

        vm.PrepareNewSnippet(folderId);

        Assert.True(vm.IsNew);
        Assert.True(vm.IsVisible);
        Assert.Equal(string.Empty, vm.Code);
        Assert.Equal(string.Empty, vm.Content);
        Assert.Equal(folderId, vm.SelectedFolderId);
        Assert.True(vm.IsActive);
        Assert.False(vm.IsFavorite);
    }

    [Fact]
    public void LoadSnippet_LoadsAllPropertiesCorrectly()
    {
        var vm = new SnippetEditorViewModel();
        var snippetId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var dto = new SnippetDto(
            snippetId,
            folderId,
            "sig",
            "Firma",
            "Atentamente, Juan",
            CaseSensitive: true,
            IsActive: true,
            IsFavorite: true,
            IsInTrash: false,
            DeletedAt: null,
            SortOrder: 1,
            UsageCount: 5,
            LastUsedAt: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow);

        vm.LoadSnippet(dto);

        Assert.False(vm.IsNew);
        Assert.True(vm.IsVisible);
        Assert.Equal(snippetId, vm.Id);
        Assert.Equal("sig", vm.Code);
        Assert.Equal("Firma", vm.Name);
        Assert.Equal("Atentamente, Juan", vm.Content);
        Assert.True(vm.CaseSensitive);
        Assert.True(vm.IsFavorite);
        Assert.Equal(5, vm.UsageCount);
    }

    [Fact]
    public void Validate_WhenCodeIsEmpty_ReturnsFalseAndSetsError()
    {
        var vm = new SnippetEditorViewModel();
        vm.Code = "   ";

        bool isValid = vm.Validate();

        Assert.False(isValid);
        Assert.True(vm.HasError);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public void Validate_WhenCodeExceedsMax_ReturnsFalseAndSetsError()
    {
        var vm = new SnippetEditorViewModel();
        vm.Code = new string('x', 51);

        bool isValid = vm.Validate();

        Assert.False(isValid);
        Assert.True(vm.HasError);
    }

    [Fact]
    public void InsertVariable_AppendsTagToContent()
    {
        var vm = new SnippetEditorViewModel();
        vm.Content = "Fecha: ";

        vm.InsertVariable("{date}");

        Assert.Equal("Fecha: {date}", vm.Content);
    }

    [Fact]
    public void UpdateFolders_BuildsIndentedHierarchy()
    {
        var vm = new SnippetEditorViewModel();
        var rootId = Guid.NewGuid();
        var subId = Guid.NewGuid();

        var folders = new List<FolderDto>
        {
            new(rootId, "Trabajo", null, 0, DateTime.UtcNow, DateTime.UtcNow),
            new(subId, "Proyectos", rootId, 0, DateTime.UtcNow, DateTime.UtcNow)
        };

        vm.UpdateFolders(folders);

        Assert.Equal(3, vm.AvailableFolders.Count); // Root null + Trabajo + Proyectos
        Assert.Equal("(Sin carpeta raíz)", vm.AvailableFolders[0].DisplayName);
        Assert.Contains("Trabajo", vm.AvailableFolders[1].DisplayName);
        Assert.Contains("Proyectos", vm.AvailableFolders[2].DisplayName);
        Assert.Contains("↳", vm.AvailableFolders[2].DisplayName);
    }

    [Fact]
    public void SaveCommand_WhenValid_TriggersOnSaveRequested()
    {
        var vm = new SnippetEditorViewModel();
        vm.PrepareNewSnippet();
        vm.Code = "bd";
        vm.Content = "Buenos días";

        bool saveCalled = false;
        vm.OnSaveRequested += _ =>
        {
            saveCalled = true;
            return Task.CompletedTask;
        };

        vm.SaveCommand.Execute(null);

        Assert.True(saveCalled);
    }

    [Fact]
    public void SettingErrorMessage_RaisesPropertyChangedForHasError()
    {
        var vm = new SnippetEditorViewModel();
        var raisedProperties = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
            {
                raisedProperties.Add(e.PropertyName);
            }
        };

        vm.ErrorMessage = "El código ya existe.";

        Assert.True(vm.HasError);
        Assert.Contains(nameof(vm.HasError), raisedProperties);
        Assert.Contains(nameof(vm.ErrorMessage), raisedProperties);

        raisedProperties.Clear();
        vm.ErrorMessage = null;

        Assert.False(vm.HasError);
        Assert.Contains(nameof(vm.HasError), raisedProperties);
    }
}
