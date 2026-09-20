namespace AtajaMe.Domain.Entities;

/// <summary>
/// Carpeta organizativa para agrupar atajos. Admite jerarquías de hasta 3 niveles.
/// </summary>
public sealed class Folder
{
    public const int MaxHierarchyDepth = 3;

    public Guid Id { get; }
    public string Name { get; private set; }
    public Guid? ParentId { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    public Folder(Guid id, string name, Guid? parentId = null, int sortOrder = 0, DateTime? createdAt = null, DateTime? updatedAt = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre de la carpeta no puede estar vacío.", nameof(name));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Name = name.Trim();
        ParentId = parentId;
        SortOrder = sortOrder;
        var now = DateTime.UtcNow;
        CreatedAt = createdAt ?? now;
        UpdatedAt = updatedAt ?? now;
    }

    public static Folder Create(string name, Guid? parentId = null, int sortOrder = 0)
    {
        return new Folder(Guid.NewGuid(), name, parentId, sortOrder);
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("El nombre de la carpeta no puede estar vacío.", nameof(newName));
        }

        Name = newName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void MoveTo(Guid? newParentId)
    {
        if (newParentId == Id)
        {
            throw new InvalidOperationException("Una carpeta no puede ser su propio padre.");
        }

        ParentId = newParentId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSortOrder(int newSortOrder)
    {
        SortOrder = newSortOrder;
        UpdatedAt = DateTime.UtcNow;
    }
}
