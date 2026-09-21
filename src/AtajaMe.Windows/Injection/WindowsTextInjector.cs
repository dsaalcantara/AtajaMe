using System.Runtime.InteropServices;
using AtajaMe.Application.Services;
using AtajaMe.Windows.Native;

namespace AtajaMe.Windows.Injection;

/// <summary>
/// Inyector de texto nativo mediante SendInput y eventos Unicode.
/// Borra el código y delimitador disparadores con pulsaciones de retroceso (VK_BACK)
/// y escribe la expansión sin alterar el portapapeles del sistema (RF-06).
/// </summary>
public sealed class WindowsTextInjector : ITextInjector
{
    private readonly IWin32Api _win32;

    public WindowsTextInjector(IWin32Api? win32 = null)
    {
        _win32 = win32 ?? NativeWin32Api.Instance;
    }

    public Task<bool> InjectAsync(
        int backspacesToDelete,
        string textToInject,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(false);
        }

        backspacesToDelete = Math.Max(0, backspacesToDelete);
        textToInject ??= string.Empty;

        int totalEvents = (backspacesToDelete * 2) + (textToInject.Length * 2);
        if (totalEvents == 0)
        {
            return Task.FromResult(true);
        }

        var inputs = new INPUT[totalEvents];
        int index = 0;

        // 1. Borrado del código disparador y delimitador con pulsaciones atómicas de VK_BACK
        for (int i = 0; i < backspacesToDelete; i++)
        {
            // KeyDown VK_BACK
            inputs[index++] = CreateKeyInput(
                vk: Win32Constants.VK_BACK,
                scan: 0,
                flags: 0);

            // KeyUp VK_BACK
            inputs[index++] = CreateKeyInput(
                vk: Win32Constants.VK_BACK,
                scan: 0,
                flags: Win32Constants.KEYEVENTF_KEYUP);
        }

        // 2. Inyección de texto plano mediante eventos de caracteres Unicode
        foreach (char c in textToInject)
        {
            // KeyDown Unicode
            inputs[index++] = CreateKeyInput(
                vk: 0,
                scan: (ushort)c,
                flags: Win32Constants.KEYEVENTF_UNICODE);

            // KeyUp Unicode
            inputs[index++] = CreateKeyInput(
                vk: 0,
                scan: (ushort)c,
                flags: Win32Constants.KEYEVENTF_UNICODE | Win32Constants.KEYEVENTF_KEYUP);
        }

        // Envío atómico a la cola de entrada del sistema operativo
        int inputSize = Marshal.SizeOf<INPUT>();
        uint sent = _win32.SendInput((uint)inputs.Length, inputs, inputSize);

        bool success = sent == inputs.Length;
        return Task.FromResult(success);
    }

    private static INPUT CreateKeyInput(ushort vk, ushort scan, uint flags)
    {
        return new INPUT
        {
            type = Win32Constants.INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vk,
                    wScan = scan,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = Win32Constants.INJECTED_SIGNATURE
                }
            }
        };
    }
}
