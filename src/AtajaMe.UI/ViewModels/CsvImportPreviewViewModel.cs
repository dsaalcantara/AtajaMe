using System.Collections.ObjectModel;
using System.Windows.Input;
using AtajaMe.Application.Csv.DTOs;
using AtajaMe.Domain.Enums;
using AtajaMe.UI.Common;

namespace AtajaMe.UI.ViewModels;

public sealed class CsvCollisionItemViewModel : ViewModelBase
{
    private CsvConflictResolution _resolution;

    public CsvImportItemPreview Item { get; }
    public string Code => Item.Row.Code;
    public string ExistingName => Item.ExistingSnippet?.Name ?? string.Empty;
    public string IncomingName => Item.Row.Name;

    public CsvConflictResolution Resolution
    {
        get => _resolution;
        set => SetProperty(ref _resolution, value);
    }

    public CsvCollisionItemViewModel(CsvImportItemPreview item, CsvConflictResolution defaultResolution)
    {
        Item = item;
        _resolution = defaultResolution;
    }
}

public sealed class CsvPreviewItemViewModel : ViewModelBase
{
    public int RowIndex { get; }
    public string Code { get; }
    public string Name { get; }
    public string Content { get; }
    public string FolderPath { get; }
    public bool HasConflict { get; }

    public CsvPreviewItemViewModel(CsvImportItemPreview item)
    {
        RowIndex = item.RowIndex;
        Code = item.Row.Code;
        Name = string.IsNullOrWhiteSpace(item.Row.Name) ? item.Row.Code : item.Row.Name;
        Content = item.Row.Content;
        FolderPath = string.IsNullOrWhiteSpace(item.Row.FolderPath) ? "(Raíz)" : item.Row.FolderPath;
        HasConflict = item.HasConflict;
    }
}

/// <summary>
/// ViewModel para la previsualización y gestión de colisiones al importar CSV (RF-26).
/// </summary>
public sealed class CsvImportPreviewViewModel : ViewModelBase
{
    private readonly CsvImportPreview _preview;
    private CsvConflictResolution _defaultResolution = CsvConflictResolution.Skip;
    private bool _isConfirmed;
    private int _selectedTabIndex;

    public int TotalRows => _preview.TotalRows;
    public int ValidSnippetsCount => _preview.ValidRows;
    public int CollisionsCount => _preview.ConflictCount;
    public int ErrorsCount => _preview.InvalidRows;

    public bool HasCollisions => CollisionsCount > 0;
    public bool HasErrors => ErrorsCount > 0;
    public bool HasValidSnippets => ValidSnippetsCount > 0;
    public bool CanImport => ValidSnippetsCount > 0;

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public CsvConflictResolution DefaultResolution
    {
        get => _defaultResolution;
        set
        {
            if (SetProperty(ref _defaultResolution, value))
            {
                foreach (var item in Collisions)
                {
                    item.Resolution = value;
                }
            }
        }
    }

    public bool IsConfirmed => _isConfirmed;

    public ObservableCollection<CsvPreviewItemViewModel> ValidSnippets { get; } = new();
    public ObservableCollection<CsvCollisionItemViewModel> Collisions { get; } = new();
    public ObservableCollection<string> Errors { get; } = new();

    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }

    public event Action? RequestClose;

    public CsvImportPreviewViewModel(CsvImportPreview preview)
    {
        _preview = preview;

        foreach (var item in preview.Items.Where(i => i.IsValid))
        {
            ValidSnippets.Add(new CsvPreviewItemViewModel(item));
        }

        foreach (var item in preview.Items.Where(i => i.HasConflict))
        {
            Collisions.Add(new CsvCollisionItemViewModel(item, _defaultResolution));
        }

        foreach (var err in preview.Items.Where(i => !i.IsValid))
        {
            Errors.Add($"Fila {err.RowIndex}: {err.ValidationErrorMessage}");
        }

        if (Collisions.Count > 0)
        {
            _selectedTabIndex = 1;
        }
        else if (ValidSnippets.Count > 0)
        {
            _selectedTabIndex = 0;
        }
        else
        {
            _selectedTabIndex = 2;
        }

        ConfirmCommand = new RelayCommand(ExecuteConfirm);
        CancelCommand = new RelayCommand(ExecuteCancel);
    }

    public Dictionary<string, CsvConflictResolution> GetResolutionOverrides()
    {
        var dict = new Dictionary<string, CsvConflictResolution>(StringComparer.OrdinalIgnoreCase);
        foreach (var col in Collisions)
        {
            dict[col.Code] = col.Resolution;
        }
        return dict;
    }

    private void ExecuteConfirm()
    {
        _isConfirmed = true;
        RequestClose?.Invoke();
    }

    private void ExecuteCancel()
    {
        _isConfirmed = false;
        RequestClose?.Invoke();
    }
}
