using System.Windows;
using System.Windows.Forms;
using AtajaMe.Domain.Updates;
using AtajaMe.Application.Updates;
using AtajaMe.UI.ViewModels;

namespace AtajaMe.UI.Views;

public partial class SettingsDialog : Window
{
    public SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

    public SettingsDialog(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.RequestClose += () => Close();
        viewModel.RequestOpenConflictResolution += OpenConflictResolutionDialog;
        viewModel.RequestShowUpdateDialog += ShowUpdateDialog;

        Loaded += async (_, _) => await viewModel.InitializeAsync();
    }

    private void ShowUpdateDialog(UpdateInfo updateInfo, IUpdateDownloader downloader)
    {
        var dialog = new UpdateAvailableDialog(updateInfo, downloader)
        {
            Owner = this
        };
        dialog.ShowDialog();
    }

    private void OnBrowseSyncFolderClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Selecciona la carpeta compartida en la nube para sincronizar AtajaMe",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            ViewModel.SyncFolderPath = dialog.SelectedPath;
        }
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnOpenConflictResolutionClick(object sender, RoutedEventArgs e)
    {
        OpenConflictResolutionDialog();
    }

    private void OpenConflictResolutionDialog()
    {
        var app = (App)System.Windows.Application.Current;
        app.OpenConflictResolutionDialog(this);
    }
}
