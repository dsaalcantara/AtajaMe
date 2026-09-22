namespace AtajaMe.UI.Services;

/// <summary>
/// Contrato para la interacción visual en la gestión de carpetas (creación, renombramiento y confirmación de borrado).
/// Desacopla la lógica de presentación en ViewModel de las ventanas de diálogo WPF.
/// </summary>
public interface IFolderDialogService
{
    /// <summary>
    /// Muestra un diálogo solicitando al usuario el nombre de una carpeta o subcarpeta.
    /// </summary>
    /// <param name="title">Título de la ventana de diálogo.</param>
    /// <param name="prompt">Texto o instrucción mostrada al usuario.</param>
    /// <param name="initialValue">Nombre sugerido inicial.</param>
    /// <returns>El nombre introducido o null si el usuario canceló la acción.</returns>
    Task<string?> PromptFolderNameAsync(string title, string prompt, string initialValue);

    /// <summary>
    /// Solicita confirmación al usuario antes de eliminar una carpeta.
    /// </summary>
    /// <param name="folderName">Nombre de la carpeta a eliminar.</param>
    /// <param name="snippetCount">Cantidad de atajos que contiene la carpeta.</param>
    /// <returns>True si el usuario confirma la eliminación; False en caso contrario.</returns>
    Task<bool> ConfirmDeleteFolderAsync(string folderName, int snippetCount);
}
