using System.Diagnostics;
using System.Windows;
using AtajaMe.Application.Updates;
using AtajaMe.Domain.Updates;
using WpfMessageBox = System.Windows.MessageBox;

namespace AtajaMe.UI.Views;

public partial class UpdateAvailableDialog : Window
{
    private readonly UpdateInfo _updateInfo;
    private readonly IUpdateDownloader _updateDownloader;

    public UpdateAvailableDialog(UpdateInfo updateInfo, IUpdateDownloader updateDownloader)
    {
        InitializeComponent();
        _updateInfo = updateInfo;
        _updateDownloader = updateDownloader;

        VersionText.Text = $"Versión actual: v{_updateInfo.CurrentVersion}  ➜  Nueva versión: {_updateInfo.TagName}";
        ReleaseNotesTextBox.Text = string.IsNullOrWhiteSpace(_updateInfo.ReleaseNotes)
            ? "No se han proporcionado notas detalladas para esta versión."
            : _updateInfo.ReleaseNotes;

        if (string.IsNullOrWhiteSpace(_updateInfo.DownloadUrl))
        {
            DownloadInstallButton.IsEnabled = false;
            DownloadInstallButton.ToolTip = "El archivo instalador no está disponible directamente en los adjuntos del release. Pulsa 'Ver en GitHub' para descargarlo manualmente.";
        }
    }

    private void OnViewOnGitHubClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _updateInfo.ReleasePageUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show($"No se pudo abrir la página de GitHub: {ex.Message}", "AtajaMe", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnRemindLaterClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnDownloadInstallClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_updateInfo.DownloadUrl))
        {
            return;
        }

        DownloadInstallButton.IsEnabled = false;
        RemindLaterButton.IsEnabled = false;
        ProgressPanel.Visibility = Visibility.Visible;
        DownloadProgressBar.Value = 0;

        var progress = new Progress<double>(percentage =>
        {
            Dispatcher.Invoke(() =>
            {
                DownloadProgressBar.Value = Math.Round(percentage * 100, 0);
                ProgressStatusText.Text = $"Descargando actualización: {DownloadProgressBar.Value}%";
            });
        });

        try
        {
            var installerPath = await _updateDownloader.DownloadInstallerAsync(_updateInfo.DownloadUrl, progress);

            ProgressStatusText.Text = "Iniciando instalador...";
            await Task.Delay(500);

            // Iniciar el instalador de Inno Setup
            Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true
            });

            // Cierre ordenado (hook de teclado, bandeja, sincronización) para que el instalador pueda reemplazar los binarios
            ((App)System.Windows.Application.Current).ExitApplication();
        }
        catch (Exception ex)
        {
            ProgressPanel.Visibility = Visibility.Collapsed;
            DownloadInstallButton.IsEnabled = true;
            RemindLaterButton.IsEnabled = true;
            WpfMessageBox.Show($"Ocurrió un error al descargar el instalador: {ex.Message}", "Error de actualización", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
