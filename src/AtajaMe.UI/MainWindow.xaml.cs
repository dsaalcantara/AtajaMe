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
    private bool _allowRealClose;

    public MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += async (_, _) => await viewModel.InitializeAsync();
    }

    /// <summary>
    /// Permite el cierre definitivo de la ventana cuando la aplicación finaliza explícitamente desde la bandeja.
    /// </summary>
    public void ForceClose()
    {
        _allowRealClose = true;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowRealClose)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }

    private void OnFolderTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is FolderTreeItemViewModel folderNode)
        {
            ViewModel.SelectedFolder = folderNode;
        }
    }

    private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
    {
        var app = (App)System.Windows.Application.Current;
        app.OpenSettingsDialog();
    }
}