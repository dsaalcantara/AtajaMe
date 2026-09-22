using System.Windows;
using AtajaMe.UI.ViewModels;

namespace AtajaMe.UI.Views;

public partial class CsvImportPreviewDialog : Window
{
    public CsvImportPreviewViewModel ViewModel => (CsvImportPreviewViewModel)DataContext;

    public CsvImportPreviewDialog(CsvImportPreviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.RequestClose += () =>
        {
            DialogResult = viewModel.IsConfirmed;
            Close();
        };
    }
}
