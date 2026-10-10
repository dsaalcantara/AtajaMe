using System.Runtime.InteropServices;
using AtajaMe.Application.Services;
using AtajaMe.Windows.Native;

namespace AtajaMe.Windows.Injection;

/// <summary>
/// Inyector de texto nativo mediante SendInput y eventos Unicode.
/// Borra el código y delimitador disparadores con pulsaciones de retroceso (VK_BACK)
/// y escribe la expansión directamente mediante eventos Unicode, sin alterar ni depender
/// del portapapeles del sistema bajo ninguna circunstancia (RF-06, RF-48, D25).
/// </summary>
public sealed class WindowsTextInjector : ITextInjector
{
    private readonly IWin32Api _win32;

    public WindowsTextInjector(IWin32Api? win32 = null)
    {
        _win32 = win32 ?? NativeWin32Api.Instance;
    }

    public async Task<bool> InjectAsync(
        int backspacesToDelete,
        string textToInject,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        backspacesToDelete = Math.Max(0, backspacesToDelete);
        textToInject ??= string.Empty;

        // 1. Liberar virtualmente modificadores que el usuario pudiera estar presionando (ej. Shift tras ':')
        await ReleaseHeldModifiersAsync(cancellationToken);

        int inputSize = Marshal.SizeOf<INPUT>();

        // 2. Borrado de código disparador y delimitador con VK_BACK
        if (backspacesToDelete > 0)
        {
            var backInputs = new INPUT[backspacesToDelete * 2];
            int bIdx = 0;
            for (int i = 0; i < backspacesToDelete; i++)
            {
                backInputs[bIdx++] = CreateKeyInput(Win32Constants.VK_BACK, 0, 0);
                backInputs[bIdx++] = CreateKeyInput(Win32Constants.VK_BACK, 0, Win32Constants.KEYEVENTF_KEYUP);
            }

            uint sentBack = _win32.SendInput((uint)backInputs.Length, backInputs, inputSize);
            if (sentBack != backInputs.Length)
            {
                return false;
            }

            if (textToInject.Length > 0)
            {
                try
                {
                    await Task.Delay(15, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
            }
        }

        if (string.IsNullOrEmpty(textToInject))
        {
            return true;
        }

        // 3. Inyección directa carácter a carácter mediante eventos Unicode SendInput.
        // Cumple estrictamente con RF-06, RF-48 y D25: CERO uso o alteración del portapapeles de Windows,
        // garantizando compatibilidad total con el Portapapeles Avanzado (Win + V) y evitando pegar
        // accidentalmente datos previos del usuario.
        var normalizedText = textToInject.Replace("\r\n", "\n").Replace('\r', '\n');
        const int chunkSize = 16;
        for (int offset = 0; offset < normalizedText.Length; offset += chunkSize)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            int count = Math.Min(chunkSize, normalizedText.Length - offset);
            var chunk = normalizedText.Substring(offset, count);

            var chunkInputs = new List<INPUT>(count * 2);
            foreach (char c in chunk)
            {
                if (c == '\n')
                {
                    chunkInputs.Add(CreateKeyInput((ushort)Win32Constants.VK_RETURN, 0, 0));
                    chunkInputs.Add(CreateKeyInput((ushort)Win32Constants.VK_RETURN, 0, Win32Constants.KEYEVENTF_KEYUP));
                }
                else
                {
                    chunkInputs.Add(CreateKeyInput(0, (ushort)c, Win32Constants.KEYEVENTF_UNICODE));
                    chunkInputs.Add(CreateKeyInput(0, (ushort)c, Win32Constants.KEYEVENTF_UNICODE | Win32Constants.KEYEVENTF_KEYUP));
                }
            }

            var arr = chunkInputs.ToArray();
            uint sent = _win32.SendInput((uint)arr.Length, arr, inputSize);
            if (sent != arr.Length)
            {
                return false;
            }

            if (offset + chunkSize < normalizedText.Length)
            {
                try
                {
                    await Task.Delay(2, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private async Task ReleaseHeldModifiersAsync(CancellationToken cancellationToken)
    {
        try
        {
            int inputSize = Marshal.SizeOf<INPUT>();
            var releaseInputs = new List<INPUT>();

            if ((_win32.GetAsyncKeyState(Win32Constants.VK_SHIFT) & 0x8000) != 0)
            {
                releaseInputs.Add(CreateKeyInput(Win32Constants.VK_SHIFT, 0, Win32Constants.KEYEVENTF_KEYUP));
            }
            if ((_win32.GetAsyncKeyState(Win32Constants.VK_CONTROL) & 0x8000) != 0)
            {
                releaseInputs.Add(CreateKeyInput(Win32Constants.VK_CONTROL, 0, Win32Constants.KEYEVENTF_KEYUP));
            }
            if ((_win32.GetAsyncKeyState(Win32Constants.VK_MENU) & 0x8000) != 0)
            {
                releaseInputs.Add(CreateKeyInput(Win32Constants.VK_MENU, 0, Win32Constants.KEYEVENTF_KEYUP));
            }

            if (releaseInputs.Count > 0)
            {
                var arr = releaseInputs.ToArray();
                _win32.SendInput((uint)arr.Length, arr, inputSize);
                await Task.Delay(5, cancellationToken);
            }
        }
        catch
        {
            // Ignorar fallos menores al liberar modificadores
        }
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
