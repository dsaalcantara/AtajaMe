using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AtajaMe.UI.Common;

/// <summary>
/// Proporciona utilidades para aplicar el modo oscuro nativo en las barras de título de Windows 10/11 vía DWM (RF-38).
/// </summary>
public static class WindowThemeHelper
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;
    private const int DWMWA_COLOR_DEFAULT = -1;

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_REFRESH = SWP_NOSIZE | SWP_NOMOVE | SWP_NOZORDER | SWP_FRAMECHANGED;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    /// <summary>
    /// Aplica el modo oscuro o claro a la barra de título de una ventana de Windows mediante DWM.
    /// </summary>
    public static void ApplyDarkTitleBar(Window window, bool isDark)
    {
        if (window == null)
        {
            return;
        }

        var helper = new WindowInteropHelper(window);
        if (helper.Handle != IntPtr.Zero)
        {
            SetDarkMode(helper.Handle, isDark);
        }
        else
        {
            window.SourceInitialized += (_, _) =>
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    SetDarkMode(hwnd, isDark);
                }
            };
        }
    }

    private static void SetDarkMode(IntPtr hwnd, bool isDark)
    {
        try
        {
            int useImmersiveDarkMode = isDark ? 1 : 0;
            int result = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int));
            if (result != 0)
            {
                // Fallback para builds anteriores de Windows 10 (1809 - 1909)
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useImmersiveDarkMode, sizeof(int));
            }

            if (isDark)
            {
                // Windows 11 (build 22000+): Color de barra de título #121214 (BGR: 0x00141212) y texto #F4F4F5 (BGR: 0x00F5F4F4)
                int captionColor = 0x00141212;
                int textColor = 0x00F5F4F4;
                DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));
                DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
            }
            else
            {
                int defaultColor = DWMWA_COLOR_DEFAULT;
                DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref defaultColor, sizeof(int));
                DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref defaultColor, sizeof(int));
            }

            // Forzar actualización inmediata del marco no-cliente de la ventana
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_REFRESH);
        }
        catch
        {
            // Ignorar en entornos donde dwmapi o user32 no admita estos atributos
        }
    }
}
