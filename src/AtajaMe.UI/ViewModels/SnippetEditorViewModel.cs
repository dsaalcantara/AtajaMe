using System.Collections.ObjectModel;
using System.Windows.Input;
using AtajaMe.Application.DTOs;
using AtajaMe.Domain.Entities;
using AtajaMe.UI.Common;

namespace AtajaMe.UI.ViewModels;

public sealed record FolderOption(Guid? Id, string DisplayName);

/// <summary>
/// ViewModel para el panel inspector/editor de atajos (RF-01 a RF-06).
/// Valida disparadores, gestiona inserción de variables y ejecuta el guardado directo.
/// </summary>
public sealed class SnippetEditorViewModel : ViewModelBase
{
    private Guid? _id;
    private string _code = string.Empty;
    private string _name = string.Empty;
    private string _content = string.Empty;
    private Guid? _selectedFolderId;
    private bool _caseSensitive;
    private bool _isActive = true;
    private bool _isFavorite;
    private bool _isInTrash;
    private int _usageCount;
    private DateTime? _lastUsedAt;

    private bool _isVisible;
    private bool _isNew;
    private string? _errorMessage;

    public Guid? Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Code
    {
        get => _code;
        set
        {
            if (SetProperty(ref _code, value))
            {
                Validate();
            }
        }
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Content
    {
        get => _content;
        set
        {
            if (SetProperty(ref _content, value))
            {
                OnPropertyChanged(nameof(ContentLengthInfo));
                Validate();
            }
        }
    }

    public string ContentLengthInfo => $"{_content.Length:N0} / {Snippet.MaxContentLength:N0} caracteres";

    public Guid? SelectedFolderId
    {
        get => _selectedFolderId;
        set => SetProperty(ref _selectedFolderId, value);
    }

    public bool CaseSensitive
    {
        get => _caseSensitive;
        set => SetProperty(ref _caseSensitive, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        set => SetProperty(ref _isFavorite, value);
    }

    public bool IsInTrash
    {
        get => _isInTrash;
        set => SetProperty(ref _isInTrash, value);
    }

    public int UsageCount
    {
        get => _usageCount;
        set => SetProperty(ref _usageCount, value);
    }

    public DateTime? LastUsedAt
    {
        get => _lastUsedAt;
        set => SetProperty(ref _lastUsedAt, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public bool IsNew
    {
        get => _isNew;
        set => SetProperty(ref _isNew, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool HasError => !string.IsNullOrEmpty(_errorMessage);

    public ObservableCollection<FolderOption> AvailableFolders { get; } = new();

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand InsertVariableCommand { get; }

    public event Func<SnippetEditorViewModel, Task>? OnSaveRequested;
    public event Action? OnCancelRequested;

    public SnippetEditorViewModel()
    {
        SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync, CanSave);
        CancelCommand = new RelayCommand(ExecuteCancel);
        InsertVariableCommand = new RelayCommand(p => InsertVariable(p as string));
    }

    public void LoadSnippet(SnippetDto snippet)
    {
        Id = snippet.Id;
        Code = snippet.Code;
        Name = snippet.Name;
        Content = snippet.Content;
        SelectedFolderId = snippet.FolderId;
        CaseSensitive = snippet.CaseSensitive;
        IsActive = snippet.IsActive;
        IsFavorite = snippet.IsFavorite;
        IsInTrash = snippet.IsInTrash;
        UsageCount = snippet.UsageCount;
        LastUsedAt = snippet.LastUsedAt;

        IsNew = false;
        IsVisible = true;
        ErrorMessage = null;
    }

    public void PrepareNewSnippet(Guid? defaultFolderId = null)
    {
        Id = null;
        Code = string.Empty;
        Name = string.Empty;
        Content = string.Empty;
        SelectedFolderId = defaultFolderId;
        CaseSensitive = false;
        IsActive = true;
        IsFavorite = false;
        IsInTrash = false;
        UsageCount = 0;
        LastUsedAt = null;

        IsNew = true;
        IsVisible = true;
        ErrorMessage = null;
    }

    public void Clear()
    {
        IsVisible = false;
        IsNew = false;
        ErrorMessage = null;
    }

    public void UpdateFolders(IEnumerable<FolderDto> folders)
    {
        AvailableFolders.Clear();
        AvailableFolders.Add(new FolderOption(null, "(Sin carpeta raíz)"));

        var folderList = folders.ToList();
        // Construcción jerárquica con sangría
        var roots = folderList.Where(f => f.ParentId == null).OrderBy(f => f.SortOrder).ThenBy(f => f.Name);
        foreach (var root in roots)
        {
            AddFolderRecursive(root, folderList, 0);
        }
    }

    private void AddFolderRecursive(FolderDto current, List<FolderDto> all, int level)
    {
        var prefix = new string(' ', level * 4);
        var icon = level == 0 ? "📁 " : "↳ 📁 ";
        AvailableFolders.Add(new FolderOption(current.Id, $"{prefix}{icon}{current.Name}"));

        var children = all.Where(f => f.ParentId == current.Id).OrderBy(f => f.SortOrder).ThenBy(f => f.Name);
        foreach (var child in children)
        {
            AddFolderRecursive(child, all, level + 1);
        }
    }

    public void InsertVariable(string? tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return;
        }

        Content += tag;
    }

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            ErrorMessage = "El código del atajo no puede estar vacío.";
            return false;
        }

        if (Code.Trim().Length > Snippet.MaxCodeLength)
        {
            ErrorMessage = $"El código no puede superar los {Snippet.MaxCodeLength} caracteres.";
            return false;
        }

        if (Content.Length > Snippet.MaxContentLength)
        {
            ErrorMessage = $"El contenido supera el límite máximo de {Snippet.MaxContentLength:N0} caracteres.";
            return false;
        }

        ErrorMessage = null;
        return true;
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Code) && !HasError;

    private async Task ExecuteSaveAsync()
    {
        if (!Validate())
        {
            return;
        }

        if (OnSaveRequested != null)
        {
            await OnSaveRequested.Invoke(this);
        }
    }

    private void ExecuteCancel()
    {
        OnCancelRequested?.Invoke();
    }
}
