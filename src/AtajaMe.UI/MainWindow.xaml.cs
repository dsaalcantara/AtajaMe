using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using AtajaMe.UI.ViewModels;

namespace AtajaMe.UI;

/// <summary>
/// Ventana principal de AtajaMe.
/// Permite gestionar carpetas, atajos, edición y estado del orquestador en segundo plano (RF-01 a RF-19).
/// Al cerrar la ventana, se oculta en la bandeja del sistema (RF-34 y RF-35).
/// </summary>
public partial class MainWindow : Window
{
    private const int WmMouseHWheel = 0x020E;
    private HwndSource? _hwndSource;

    public MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += async (_, _) => await viewModel.InitializeAsync();
        PreviewMouseWheel += OnWindowPreviewMouseWheel;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        _hwndSource?.AddHook(WndProc);
    }

    protected override void OnClosed(EventArgs e)
    {
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource = null;
        base.OnClosed(e);
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

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmMouseHWheel)
        {
            int tilt = (short)((wParam.ToInt64() >> 16) & 0xFFFF);
            if (tilt != 0 && Mouse.DirectlyOver is DependencyObject overElement)
            {
                var scrollViewer = FindHorizontalScrollViewer(overElement);
                if (scrollViewer != null)
                {
                    double step = (tilt / 120.0) * 36.0;
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + step);
                    handled = true;
                }
            }
        }

        return IntPtr.Zero;
    }

    private void OnWindowPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Shift && e.OriginalSource is DependencyObject sourceElement)
        {
            var scrollViewer = FindHorizontalScrollViewer(sourceElement);
            if (scrollViewer != null)
            {
                double step = (-e.Delta / 120.0) * 36.0;
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + step);
                e.Handled = true;
            }
        }
    }

    private static ScrollViewer? FindHorizontalScrollViewer(DependencyObject? current)
    {
        while (current != null)
        {
            if (current is ScrollViewer sv && sv.ScrollableWidth > 0)
            {
                return sv;
            }

            current = current is Visual || current is System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }

        return null;
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

    private void OnTreeViewItemRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
    {
        // Evitar el autodesplazamiento horizontal brusco de WPF al seleccionar carpetas largas o anidadas
        e.Handled = true;
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