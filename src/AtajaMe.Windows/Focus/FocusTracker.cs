using System.Diagnostics;
using AtajaMe.Application.Services;
using AtajaMe.Windows.Native;

namespace AtajaMe.Windows.Focus;

/// <summary>
/// Seguimiento del foco del sistema operativo y ventana en primer plano.
/// Permite validar cambios de ventana activa (RF-10) y extraer el proceso para exclusiones (RF-07).
/// </summary>
public sealed class FocusTracker : IFocusTracker
{
    private readonly IWin32Api _win32;

    public FocusTracker(IWin32Api? win32 = null)
    {
        _win32 = win32 ?? NativeWin32Api.Instance;
    }

    public IntPtr GetActiveWindowHandle()
    {
        return _win32.GetForegroundWindow();
    }

    public string? GetActiveProcessName()
    {
        var hWnd = GetActiveWindowHandle();
        if (hWnd == IntPtr.Zero)
        {
            return null;
        }

        _win32.GetWindowThreadProcessId(hWnd, out uint processId);
        if (processId == 0)
        {
            return null;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch
        {
            // El proceso puede haber terminado justo antes de consultar
            return null;
        }
    }

    public bool HasFocusChanged(IntPtr originalWindowHandle)
    {
        if (originalWindowHandle == IntPtr.Zero)
        {
            return true;
        }

        var currentWindow = GetActiveWindowHandle();
        if (currentWindow == originalWindowHandle)
        {
            return false;
        }

        if (currentWindow == IntPtr.Zero)
        {
            return true;
        }

        // Si el identificador de ventana cambió pero sigue perteneciendo al mismo proceso
        // (por ejemplo, al teclear '@' muchas aplicaciones abren un menú o popup de autocompletado/menciones),
        // no considerarlo pérdida de foco entre aplicaciones (RF-10).
        _win32.GetWindowThreadProcessId(originalWindowHandle, out uint origPid);
        _win32.GetWindowThreadProcessId(currentWindow, out uint currPid);

        if (origPid != 0 && origPid == currPid)
        {
            return false;
        }

        return true;
    }

    public bool IsCurrentAppActive()
    {
        var hWnd = GetActiveWindowHandle();
        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        _win32.GetWindowThreadProcessId(hWnd, out uint processId);
        if (processId == 0)
        {
            return false;
        }

        return processId == (uint)Environment.ProcessId;
    }
}
