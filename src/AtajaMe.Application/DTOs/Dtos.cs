using AtajaMe.Domain.Entities;

namespace AtajaMe.Application.DTOs;

public sealed record SnippetDto(
    Guid Id,
    Guid? FolderId,
    string Code,
    string Name,
    string Content,
    bool CaseSensitive,
    bool IsActive,
    bool IsFavorite,
    bool IsInTrash,
    DateTime? DeletedAt,
    int SortOrder,
    int UsageCount,
    DateTime? LastUsedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static SnippetDto FromDomain(Snippet snippet) => new(
        snippet.Id,
        snippet.FolderId,
        snippet.Code,
        snippet.Name,
        snippet.Content,
        snippet.CaseSensitive,
        snippet.IsActive,
        snippet.IsFavorite,
        snippet.IsInTrash,
        snippet.DeletedAt,
        snippet.SortOrder,
        snippet.UsageCount,
        snippet.LastUsedAt,
        snippet.CreatedAt,
        snippet.UpdatedAt
    );
}

public sealed record FolderDto(
    Guid Id,
    string Name,
    Guid? ParentId,
    int SortOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static FolderDto FromDomain(Folder folder) => new(
        folder.Id,
        folder.Name,
        folder.ParentId,
        folder.SortOrder,
        folder.CreatedAt,
        folder.UpdatedAt
    );
}

public sealed record ExcludedApplicationDto(
    Guid Id,
    string ProcessName,
    string? Description,
    DateTime CreatedAt)
{
    public static ExcludedApplicationDto FromDomain(ExcludedApplication app) => new(
        app.Id,
        app.ProcessName,
        app.Description,
        app.CreatedAt
    );
}

public sealed record CreateSnippetCommand(
    string Code,
    string Name,
    string Content,
    Guid? FolderId = null,
    bool CaseSensitive = false);

public sealed record UpdateSnippetCommand(
    Guid Id,
    string Code,
    string Name,
    string Content,
    Guid? FolderId = null,
    bool CaseSensitive = false);

public sealed record CreateFolderCommand(
    string Name,
    Guid? ParentId = null,
    int SortOrder = 0);

public sealed record UpdateFolderCommand(
    Guid Id,
    string Name,
    Guid? ParentId = null,
    int SortOrder = 0);
