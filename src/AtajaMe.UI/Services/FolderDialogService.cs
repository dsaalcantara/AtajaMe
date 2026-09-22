using System.Windows;
using AtajaMe.UI.Views;

namespace AtajaMe.UI.Services;

/// <summary>
/// Implementación de IFolderDialogService basada en ventanas WPF y MessageBox nativo.
/// </summary>
public sealed class FolderDialogService : IFolderDialogService
{
    public Task<string?> PromptFolderNameAsync(string title, string prompt, string initialValue)
    {
        return System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new FolderInputDialog(title, prompt, initialValue)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            var result = dialog.ShowDialog();
            if (result == true)
            {
                return Task.FromResult<string?>(dialog.FolderName.Trim());
            }

            return Task.FromResult<string?>(null);
        });
    }

    public Task<bool> ConfirmDeleteFolderAsync(string folderName, int snippetCount)
    {
        return System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            string message = snippetCount > 0
                ? $"¿Estás seguro de que deseas eliminar la carpeta \"{folderName}\"?\n\nContiene {snippetCount} atajo(s) que no se perderán; se conservarán en \"Todos los atajos\"."
                : $"¿Estás seguro de que deseas eliminar la carpeta \"{folderName}\"?";

            var result = System.Windows.MessageBox.Show(
                System.Windows.Application.Current.MainWindow,
                message,
                "Eliminar carpeta",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question,
                System.Windows.MessageBoxResult.No);

            return Task.FromResult(result == System.Windows.MessageBoxResult.Yes);
        });
    }
}
