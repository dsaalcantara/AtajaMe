using System.Windows;
using AtajaMe.UI.ViewModels;

namespace AtajaMe.UI.Views;

public partial class ConflictResolutionDialog : Window
{
    public ConflictResolutionViewModel ViewModel => (ConflictResolutionViewModel)DataContext;

    public ConflictResolutionDialog(ConflictResolutionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.RequestClose += () => Close();
        Loaded += async (_, _) => await viewModel.InitializeAsync();
    }
}
