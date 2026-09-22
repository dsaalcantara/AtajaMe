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

        int inputSize = Marshal.SizeOf<INPUT>();

        // 1. Borrado de código disparador y delimitador
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

            // Breve intervalo para que el control de Windows procese el borrado
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

        // 2. Inyección de texto Unicode
        // Normalizar saltos de línea Windows
        var normalizedText = textToInject.Replace("\r\n", "\n").Replace('\r', '\n');

        // Enviar en lotes pequeños (máximo 16 caracteres por llamada a SendInput)
        // para asegurar que ningún KeyUp se pierda ni sature el buffer de Windows
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

            // Si hay más lotes por enviar, micro-pausa de 2 ms para flujo continuo
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
