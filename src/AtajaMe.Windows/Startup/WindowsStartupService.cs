using System.Diagnostics;
using AtajaMe.Application.Services;
using Microsoft.Win32;

namespace AtajaMe.Windows.Startup;

/// <summary>
/// Gestiona la entrada de inicio automático en el registro de Windows (HKCU\...\Run) según RF-33.
/// </summary>
public sealed class WindowsStartupService : IWindowsStartupService
{
    private const string RunRegistrySubKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "AtajaMe";

    public bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistrySubKey, writable: false);
            return key?.GetValue(ValueName) != null;
        }
        catch
        {
            return false;
        }
    }

    public void SetStartup(bool enable, bool startMinimized = false)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistrySubKey, writable: true);
            if (key == null)
            {
                return;
            }

            if (enable)
            {
                var exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath))
                {
                    using var current = Process.GetCurrentProcess();
                    exePath = current.MainModule?.FileName;
                }

                if (!string.IsNullOrEmpty(exePath))
                {
                    var command = $"\"{exePath}\"";
                    if (startMinimized)
                    {
                        command += " --minimized";
                    }

                    key.SetValue(ValueName, command, RegistryValueKind.String);
                }
            }
            else
            {
                if (key.GetValue(ValueName) != null)
                {
                    key.DeleteValue(ValueName, throwOnMissingValue: false);
                }
            }
        }
        catch
        {
            // Evitar fallos si las políticas de grupo impiden escribir en el registro
        }
    }
}
