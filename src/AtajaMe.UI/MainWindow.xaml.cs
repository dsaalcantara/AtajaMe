using System.ComponentModel;
using System.Windows;
using AtajaMe.UI.ViewModels;

namespace AtajaMe.UI;

/// <summary>
/// Ventana principal de AtajaMe.
/// Permite gestionar carpetas, atajos, edición y estado del orquestador en segundo plano (RF-01 a RF-19).
/// Al cerrar la ventana, se oculta en la bandeja del sistema (RF-34 y RF-35).
/// </summary>
public partial class MainWindow : Window
{
    public MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += async (_, _) => await viewModel.InitializeAsync();
    }

    /// <summary>
    /// Cierra la ventana principal liberando sus recursos visuales.
    /// </summary>
    public void ForceClose()
    {
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
    }

    private void OnFolderTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is FolderTreeItemViewModel folderNode)
        {
            ViewModel.SelectedFolder = folderNode;
        }
    }

    private void OnTreeViewItemPreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.TreeViewItem treeViewItem)
        {
            treeViewItem.Focus();
            treeViewItem.IsSelected = true;
        }
    }

    private void OnFolderTreeKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (ViewModel.SelectedFolder == null)
        {
            return;
        }

        if (e.Key == System.Windows.Input.Key.F2 && ViewModel.RenameFolderCommand.CanExecute(ViewModel.SelectedFolder))
        {
            ViewModel.RenameFolderCommand.Execute(ViewModel.SelectedFolder);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Delete && ViewModel.DeleteFolderCommand.CanExecute(ViewModel.SelectedFolder))
        {
            ViewModel.DeleteFolderCommand.Execute(ViewModel.SelectedFolder);
            e.Handled = true;
        }
    }

    private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
    {
        var app = (App)System.Windows.Application.Current;
        app.OpenSettingsDialog();
    }

    private void OnInsertVariableButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.ContextMenu != null)
        {
            element.ContextMenu.DataContext = element.DataContext;
            element.ContextMenu.PlacementTarget = element;
            element.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            element.ContextMenu.IsOpen = true;
        }
    }
}