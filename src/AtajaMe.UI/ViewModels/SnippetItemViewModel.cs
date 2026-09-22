using AtajaMe.Application.DTOs;
using AtajaMe.UI.Common;

namespace AtajaMe.UI.ViewModels;

/// <summary>
/// Modelo de vista para un atajo individual mostrado en la lista central (RF-01 a RF-06).
/// </summary>
public sealed class SnippetItemViewModel : ViewModelBase
{
    private string _code = string.Empty;
    private string _name = string.Empty;
    private string _content = string.Empty;
    private string? _folderName;
    private Guid? _folderId;
    private bool _caseSensitive;
    private bool _isActive;
    private bool _isFavorite;
    private bool _isInTrash;
    private int _usageCount;
    private DateTime? _lastUsedAt;

    public Guid Id { get; }

    public string Code
    {
        get => _code;
        set => SetProperty(ref _code, value);
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
                OnPropertyChanged(nameof(ContentPreview));
            }
        }
    }

    public string ContentPreview
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_content))
            {
                return string.Empty;
            }

            var singleLine = _content.Replace("\r\n", " ").Replace("\n", " ").Trim();
            return singleLine.Length > 85 ? singleLine[..82] + "..." : singleLine;
        }
    }

    public Guid? FolderId
    {
        get => _folderId;
        set => SetProperty(ref _folderId, value);
    }

    public string? FolderName
    {
        get => _folderName;
        set => SetProperty(ref _folderName, value);
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

    public SnippetItemViewModel(SnippetDto dto, string? folderName = null)
    {
        Id = dto.Id;
        UpdateFromDto(dto, folderName);
    }

    public void UpdateFromDto(SnippetDto dto, string? folderName = null)
    {
        Code = dto.Code;
        Name = dto.Name;
        Content = dto.Content;
        FolderId = dto.FolderId;
        if (folderName != null)
        {
            FolderName = folderName;
        }
        CaseSensitive = dto.CaseSensitive;
        IsActive = dto.IsActive;
        IsFavorite = dto.IsFavorite;
        IsInTrash = dto.IsInTrash;
        UsageCount = dto.UsageCount;
        LastUsedAt = dto.LastUsedAt;
    }
}
