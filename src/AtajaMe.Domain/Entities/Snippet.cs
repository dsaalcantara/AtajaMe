namespace AtajaMe.Domain.Entities;

/// <summary>
/// Representa un atajo de texto (snippet) en AtajaMe.
/// </summary>
public sealed class Snippet
{
    public const int MaxCodeLength = 50;
    public const int MaxContentLength = 100_000; // Límite razonable para texto plano

    public Guid Id { get; }
    public Guid? FolderId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Content { get; private set; }
    public bool CaseSensitive { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsFavorite { get; private set; }
    public bool IsInTrash { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int SortOrder { get; private set; }
    public int UsageCount { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    public Snippet(
        Guid id,
        string code,
        string name,
        string content,
        Guid? folderId = null,
        bool caseSensitive = false,
        bool isActive = true,
        bool isFavorite = false,
        bool isInTrash = false,
        DateTime? deletedAt = null,
        int sortOrder = 0,
        int usageCount = 0,
        DateTime? lastUsedAt = null,
        DateTime? createdAt = null,
        DateTime? updatedAt = null)
    {
        ValidateCode(code);
        ValidateContent(content);

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Code = code.Trim();
        Name = string.IsNullOrWhiteSpace(name) ? Code : name.Trim();
        Content = content;
        FolderId = folderId;
        CaseSensitive = caseSensitive;
        IsActive = isActive;
        IsFavorite = isFavorite;
        IsInTrash = isInTrash;
        DeletedAt = deletedAt;
        SortOrder = sortOrder;
        UsageCount = usageCount;
        LastUsedAt = lastUsedAt;
        var now = DateTime.UtcNow;
        CreatedAt = createdAt ?? now;
        UpdatedAt = updatedAt ?? now;
    }

    public static Snippet Create(
        string code,
        string name,
        string content,
        Guid? folderId = null,
        bool caseSensitive = false)
    {
        return new Snippet(
            Guid.NewGuid(),
            code,
            name,
            content,
            folderId,
            caseSensitive);
    }

    public void Update(string code, string name, string content, Guid? folderId, bool caseSensitive)
    {
        ValidateCode(code);
        ValidateContent(content);

        Code = code.Trim();
        Name = string.IsNullOrWhiteSpace(name) ? Code : name.Trim();
        Content = content;
        FolderId = folderId;
        CaseSensitive = caseSensitive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MoveToTrash()
    {
        IsInTrash = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RestoreFromTrash()
    {
        IsInTrash = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ToggleActive()
    {
        IsActive = !IsActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordUsage()
    {
        UsageCount++;
        LastUsedAt = DateTime.UtcNow;
        // No se altera UpdatedAt para no provocar sincronizaciones innecesarias
    }

    public Snippet Duplicate(string newCode)
    {
        ValidateCode(newCode);

        return new Snippet(
            Guid.NewGuid(),
            newCode.Trim(),
            $"{Name} (Copia)",
            Content,
            FolderId,
            CaseSensitive,
            IsActive,
            false, // Una copia no nace como favorita
            false, // Ni en papelera
            null,
            SortOrder + 1);
    }

    public bool MatchesCode(string inputCode)
    {
        if (string.IsNullOrEmpty(inputCode))
        {
            return false;
        }

        var comparison = CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        return string.Equals(Code, inputCode, comparison);
    }

    private static void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("El código del atajo no puede estar vacío ni contener solo espacios.", nameof(code));
        }

        if (code.Trim().Length > MaxCodeLength)
        {
            throw new ArgumentException($"El código no puede superar los {MaxCodeLength} caracteres.", nameof(code));
        }
    }

    private static void ValidateContent(string content)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content), "El contenido no puede ser nulo.");
        }

        if (content.Length > MaxContentLength)
        {
            throw new ArgumentException($"El contenido supera el límite máximo permitido de {MaxContentLength} caracteres.", nameof(content));
        }
    }
}
