using System.Windows.Automation;
using AtajaMe.Application.Services;

namespace AtajaMe.Windows.Security;

/// <summary>
/// Detector de campos sensibles y contraseñas mediante UI Automation.
/// Consulta AutomationElement.IsPasswordProperty para cancelar la captura
/// y purgar el búfer de forma inmediata y silenciosa (RF-08).
/// </summary>
public sealed class SensitiveControlDetector : ISensitiveControlDetector
{
    private readonly Func<bool>? _customEvaluator;

    public SensitiveControlDetector(Func<bool>? customEvaluator = null)
    {
        _customEvaluator = customEvaluator;
    }

    public bool IsSensitiveControlFocused()
    {
        if (_customEvaluator != null)
        {
            return _customEvaluator();
        }

        try
        {
            var focusedElement = AutomationElement.FocusedElement;
            if (focusedElement == null)
            {
                return false;
            }

            var isPasswordValue = focusedElement.GetCurrentPropertyValue(AutomationElement.IsPasswordProperty, true);
            if (isPasswordValue is bool isPassword && isPassword)
            {
                return true;
            }

            return false;
        }
        catch
        {
            // Ante fallos de accesibilidad o elementos no disponibles, devolver false de manera segura
            return false;
        }
    }
}
