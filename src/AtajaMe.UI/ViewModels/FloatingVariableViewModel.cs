using System.Collections.ObjectModel;
using System.Windows.Input;
using AtajaMe.UI.Common;

namespace AtajaMe.UI.ViewModels;

public sealed class VariableInputFieldViewModel : ViewModelBase
{
    private string _value = string.Empty;

    public string VariableName { get; }
    public string DisplayLabel { get; }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    public VariableInputFieldViewModel(string variableName, string displayLabel, string defaultValue = "")
    {
        VariableName = variableName;
        DisplayLabel = displayLabel;
        Value = defaultValue;
    }
}

/// <summary>
/// ViewModel para la ventana flotante interactiva de solicitud de variables (RF-12).
/// </summary>
public sealed class FloatingVariableViewModel : ViewModelBase
{
    private string _snippetCode = string.Empty;
    private string _snippetName = string.Empty;

    public string SnippetCode
    {
        get => _snippetCode;
        set => SetProperty(ref _snippetCode, value);
    }

    public string SnippetName
    {
        get => _snippetName;
        set => SetProperty(ref _snippetName, value);
    }

    public ObservableCollection<VariableInputFieldViewModel> Fields { get; } = new();

    public bool IsConfirmed { get; private set; }

    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }

    public event EventHandler? RequestClose;

    public FloatingVariableViewModel(string snippetCode, string snippetName, IEnumerable<string> variableNames)
    {
        SnippetCode = snippetCode;
        SnippetName = snippetName;

        foreach (var varName in variableNames)
        {
            var label = varName;
            if (label.StartsWith("input:", StringComparison.OrdinalIgnoreCase))
            {
                label = label[6..];
            }

            Fields.Add(new VariableInputFieldViewModel(varName, label));
        }

        ConfirmCommand = new RelayCommand(ExecuteConfirm);
        CancelCommand = new RelayCommand(ExecuteCancel);
    }

    private void ExecuteConfirm()
    {
        IsConfirmed = true;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void ExecuteCancel()
    {
        IsConfirmed = false;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    public Dictionary<string, string> GetValues()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in Fields)
        {
            dict[field.VariableName] = field.Value;
            var cleanName = field.VariableName.StartsWith("input:", StringComparison.OrdinalIgnoreCase)
                ? field.VariableName[6..]
                : field.VariableName;
            dict[cleanName] = field.Value;
        }
        return dict;
    }
}
