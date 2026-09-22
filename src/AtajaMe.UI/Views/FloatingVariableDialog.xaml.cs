using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AtajaMe.Application.Services;
using AtajaMe.UI.ViewModels;

namespace AtajaMe.UI.Views;

public partial class FloatingVariableDialog : Window
{
    private readonly ICaretLocator? _caretLocator;
    private readonly IntPtr _targetWindowHandle;
    private bool _hasFocusedFirstInput;

    public FloatingVariableViewModel ViewModel => (FloatingVariableViewModel)DataContext;

    public FloatingVariableDialog(
        FloatingVariableViewModel viewModel,
        IntPtr targetWindowHandle,
        ICaretLocator? caretLocator = null)
    {
        InitializeComponent();
        DataContext = viewModel;
        _targetWindowHandle = targetWindowHandle;
        _caretLocator = caretLocator;

        viewModel.RequestClose += (_, _) =>
        {
            DialogResult = viewModel.IsConfirmed;
            Close();
        };

        Loaded += OnWindowLoaded;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        PositionWindowNearCaret();
    }

    private void PositionWindowNearCaret()
    {
        int x = 200;
        int y = 200;

        if (_caretLocator != null)
        {
            (x, y) = _caretLocator.GetCaretOrCursorPosition(_targetWindowHandle);
        }

        // Medidas de la ventana
        double width = ActualWidth > 0 ? ActualWidth : 360;
        double height = ActualHeight > 0 ? ActualHeight : 200;

        var workArea = SystemParameters.WorkArea;

        // Ajustar para que no exceda el borde derecho
        if (x + width > workArea.Right)
        {
            x = (int)(workArea.Right - width - 10);
        }

        // Ajustar para que no exceda el borde inferior
        if (y + height > workArea.Bottom)
        {
            y = (int)(workArea.Bottom - height - 10);
        }

        if (x < workArea.Left)
        {
            x = (int)workArea.Left + 10;
        }

        if (y < workArea.Top)
        {
            y = (int)workArea.Top + 10;
        }

        Left = x;
        Top = y;
    }

    private void OnFieldTextBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (!_hasFocusedFirstInput && sender is System.Windows.Controls.TextBox textBox)
        {
            _hasFocusedFirstInput = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                textBox.Focus();
                textBox.SelectAll();
            }));
        }
    }

    private void OnWindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ViewModel.ConfirmCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            ViewModel.CancelCommand.Execute(null);
            e.Handled = true;
        }
    }
}
