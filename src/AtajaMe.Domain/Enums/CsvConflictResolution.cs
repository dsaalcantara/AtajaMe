namespace AtajaMe.Domain.Enums;

/// <summary>
/// Estrategia de resolución de conflictos al importar atajos desde un archivo CSV.
/// </summary>
public enum CsvConflictResolution
{
    /// <summary>
    /// Omite el registro entrante del CSV y conserva el atajo local existente.
    /// </summary>
    Skip = 0,

    /// <summary>
    /// Sobrescribe los datos del atajo local existente con los valores del CSV.
    /// </summary>
    Overwrite = 1,

    /// <summary>
    /// Conserva el atajo local y crea un nuevo atajo con un código alternativo garantizado (sufijo).
    /// </summary>
    DuplicateWithSuffix = 2
}
