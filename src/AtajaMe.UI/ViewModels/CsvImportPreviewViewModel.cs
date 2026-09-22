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

/// <summary>
/// ViewModel para la previsualización y gestión de colisiones al importar CSV (RF-26).
/// </summary>
public sealed class CsvImportPreviewViewModel : ViewModelBase
{
    private readonly CsvImportPreview _preview;
    private CsvConflictResolution _defaultResolution = CsvConflictResolution.Skip;
    private bool _isConfirmed;

    public int TotalRows => _preview.TotalRows;
    public int ValidSnippetsCount => _preview.ValidRows;
    public int CollisionsCount => _preview.ConflictCount;
    public int ErrorsCount => _preview.InvalidRows;

    public bool HasCollisions => CollisionsCount > 0;
    public bool HasErrors => ErrorsCount > 0;

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

    public ObservableCollection<CsvCollisionItemViewModel> Collisions { get; } = new();
    public ObservableCollection<string> Errors { get; } = new();

    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }

    public event Action? RequestClose;

    public CsvImportPreviewViewModel(CsvImportPreview preview)
    {
        _preview = preview;

        foreach (var item in preview.Items.Where(i => i.HasConflict))
        {
            Collisions.Add(new CsvCollisionItemViewModel(item, _defaultResolution));
        }

        foreach (var err in preview.Items.Where(i => !i.IsValid))
        {
            Errors.Add($"Fila {err.RowIndex}: {err.ValidationErrorMessage}");
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
