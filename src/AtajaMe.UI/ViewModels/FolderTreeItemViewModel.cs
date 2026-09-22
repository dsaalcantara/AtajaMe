using System.Collections.ObjectModel;
using AtajaMe.Application.DTOs;
using AtajaMe.UI.Common;

namespace AtajaMe.UI.ViewModels;

/// <summary>
/// Nodo jerárquico del árbol de carpetas organizativas (RF-13).
/// Soporta hasta 3 niveles de profundidad y conteo reactivo de atajos.
/// </summary>
public sealed class FolderTreeItemViewModel : ViewModelBase
{
    private string _name;
    private int _snippetCount;
    private bool _isExpanded = true;
    private bool _isSelected;

    public Guid Id { get; }
    public Guid? ParentId { get; }
    public int SortOrder { get; }
    public int Level { get; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public int SnippetCount
    {
        get => _snippetCount;
        set => SetProperty(ref _snippetCount, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public ObservableCollection<FolderTreeItemViewModel> Children { get; } = new();

    public FolderTreeItemViewModel(FolderDto dto, int level = 1)
    {
        Id = dto.Id;
        _name = dto.Name;
        ParentId = dto.ParentId;
        SortOrder = dto.SortOrder;
        Level = level;
    }

    public FolderTreeItemViewModel(Guid id, string name, Guid? parentId = null, int sortOrder = 0, int level = 1)
    {
        Id = id;
        _name = name;
        ParentId = parentId;
        SortOrder = sortOrder;
        Level = level;
    }
}
