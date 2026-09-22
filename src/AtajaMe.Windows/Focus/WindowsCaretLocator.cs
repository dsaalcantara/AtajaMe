using System.Runtime.InteropServices;
using AtajaMe.Application.Services;
using AtajaMe.Windows.Native;

namespace AtajaMe.Windows.Focus;

/// <summary>
/// Implementación de ICaretLocator para Windows utilizando la API Win32.
/// Consulta el cursor de texto mediante GetGUIThreadInfo y ClientToScreen con fallback seguro al ratón (GetCursorPos).
/// </summary>
public sealed class WindowsCaretLocator : ICaretLocator
{
    private readonly IWin32Api _win32;

    public WindowsCaretLocator(IWin32Api? win32 = null)
    {
        _win32 = win32 ?? NativeWin32Api.Instance;
    }

    public (int X, int Y) GetCaretOrCursorPosition(IntPtr windowHandle)
    {
        if (windowHandle != IntPtr.Zero)
        {
            uint threadId = _win32.GetWindowThreadProcessId(windowHandle, out _);
            if (threadId != 0)
            {
                var guiInfo = new GUITHREADINFO
                {
                    cbSize = Marshal.SizeOf<GUITHREADINFO>()
                };

                if (_win32.GetGUIThreadInfo(threadId, ref guiInfo))
                {
                    var targetHwnd = guiInfo.hwndCaret != IntPtr.Zero ? guiInfo.hwndCaret : guiInfo.hwndFocus;
                    if (targetHwnd != IntPtr.Zero)
                    {
                        var pt = new POINT(guiInfo.rcCaret.Left, guiInfo.rcCaret.Bottom);
                        if (_win32.ClientToScreen(targetHwnd, ref pt))
                        {
                            // Si las coordenadas son válidas y no son cero absoluto
                            if (pt.X > 0 || pt.Y > 0)
                            {
                                return (pt.X, pt.Y + 4);
                            }
                        }
                    }
                }
            }
        }

        // Fallback garantizado a la posición del cursor del ratón
        if (_win32.GetCursorPos(out var mousePt))
        {
            return (mousePt.X, mousePt.Y + 16);
        }

        // Si todo falla, coordenadas por defecto centradas
        return (200, 200);
    }
}
