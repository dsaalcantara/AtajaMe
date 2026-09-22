using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace AtajaMe.UI.Views;

/// <summary>
/// Diálogo modal para introducir o modificar el nombre de una carpeta (RF-13).
/// </summary>
public partial class FolderInputDialog : Window, INotifyPropertyChanged
{
    private string _folderName = string.Empty;

    public string DialogTitle { get; }
    public string PromptText { get; }

    public string FolderName
    {
        get => _folderName;
        set
        {
            if (_folderName != value)
            {
                _folderName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValid));
            }
        }
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(FolderName);

    public event PropertyChangedEventHandler? PropertyChanged;

    public FolderInputDialog(string title, string prompt, string initialValue = "")
    {
        InitializeComponent();
        DataContext = this;

        DialogTitle = title;
        PromptText = prompt;
        FolderName = initialValue;

        Loaded += (_, _) =>
        {
            FolderNameTextBox.Focus();
            FolderNameTextBox.SelectAll();
        };
    }

    private void OnTextBoxKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter && IsValid)
        {
            DialogResult = true;
            Close();
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (IsValid)
        {
            DialogResult = true;
            Close();
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
