namespace AtajaMe.Domain.Entities;

/// <summary>
/// Aplicación en la que se inhibe intencionadamente la detección y expansión de atajos.
/// </summary>
public sealed class ExcludedApplication
{
    public Guid Id { get; }
    public string ProcessName { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; }

    public ExcludedApplication(Guid id, string processName, string? description = null, DateTime? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            throw new ArgumentException("El nombre del proceso no puede estar vacío.", nameof(processName));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        ProcessName = NormalizeProcessName(processName);
        Description = description?.Trim();
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    public static ExcludedApplication Create(string processName, string? description = null)
    {
        return new ExcludedApplication(Guid.NewGuid(), processName, description);
    }

    public bool Matches(string targetProcessName)
    {
        if (string.IsNullOrWhiteSpace(targetProcessName))
        {
            return false;
        }

        return string.Equals(ProcessName, NormalizeProcessName(targetProcessName), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeProcessName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed[..^4];
        }
        return trimmed;
    }
}
