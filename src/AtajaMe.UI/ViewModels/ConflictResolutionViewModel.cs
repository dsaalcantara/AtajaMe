using System.Collections.ObjectModel;
using System.Windows.Input;
using AtajaMe.Application.Sync.Services;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Enums;
using AtajaMe.UI.Common;

namespace AtajaMe.UI.ViewModels;

public sealed class ConflictItemViewModel : ViewModelBase
{
    public SyncConflict Conflict { get; }

    public string Code => Conflict.LocalSnippet.Code;
    public string ConflictTypeDescription => Conflict.ConflictType switch
    {
        SyncConflictType.CodeCollision => "Colisión de Código",
        SyncConflictType.ConcurrentModification => "Modificación Concurrente",
        _ => "Conflicto"
    };

    public string LocalName => Conflict.LocalSnippet.Name;
    public string LocalContent => Conflict.LocalSnippet.Content;
    public DateTime LocalUpdatedAt => Conflict.LocalSnippet.UpdatedAt;

    public string RemoteName => Conflict.RemoteSnippet.Name;
    public string RemoteContent => Conflict.RemoteSnippet.Content;
    public DateTime RemoteUpdatedAt => Conflict.RemoteSnippet.UpdatedAt;

    public ConflictItemViewModel(SyncConflict conflict)
    {
        Conflict = conflict;
    }
}

/// <summary>
/// ViewModel para el diálogo comparativo y resolución de conflictos de sincronización (RF-24).
/// </summary>
public sealed class ConflictResolutionViewModel : ViewModelBase
{
    private readonly ISyncEngine _syncEngine;
    private ConflictItemViewModel? _selectedConflict;
    private string _statusMessage = string.Empty;

    public ObservableCollection<ConflictItemViewModel> Conflicts { get; } = new();

    public ConflictItemViewModel? SelectedConflict
    {
        get => _selectedConflict;
        set => SetProperty(ref _selectedConflict, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand KeepLocalCommand { get; }
    public ICommand KeepRemoteCommand { get; }
    public ICommand DuplicateBothCommand { get; }

    public event Action? RequestClose;

    public ConflictResolutionViewModel(ISyncEngine syncEngine)
    {
        _syncEngine = syncEngine;

        KeepLocalCommand = new AsyncRelayCommand(() => ResolveAsync(SyncResolutionChoice.KeepLocal), () => SelectedConflict != null);
        KeepRemoteCommand = new AsyncRelayCommand(() => ResolveAsync(SyncResolutionChoice.KeepRemote), () => SelectedConflict != null);
        DuplicateBothCommand = new AsyncRelayCommand(() => ResolveAsync(SyncResolutionChoice.DuplicateBoth), () => SelectedConflict != null);
    }

    public async Task InitializeAsync()
    {
        var list = await _syncEngine.GetPendingConflictsAsync();
        Conflicts.Clear();
        foreach (var c in list)
        {
            Conflicts.Add(new ConflictItemViewModel(c));
        }

        SelectedConflict = Conflicts.FirstOrDefault();
        StatusMessage = Conflicts.Count > 0 ? $"{Conflicts.Count} conflicto(s) pendiente(s)" : "Sin conflictos pendientes.";
    }

    private async Task ResolveAsync(SyncResolutionChoice choice)
    {
        if (SelectedConflict == null)
        {
            return;
        }

        var current = SelectedConflict;
        try
        {
            await _syncEngine.ResolveConflictAsync(current.Conflict, choice);
            Conflicts.Remove(current);
            SelectedConflict = Conflicts.FirstOrDefault();

            StatusMessage = $"Conflicto resuelto ({choice}).";

            if (Conflicts.Count == 0)
            {
                StatusMessage = "Todos los conflictos han sido resueltos.";
                RequestClose?.Invoke();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al resolver conflicto: {ex.Message}";
        }
    }
}
