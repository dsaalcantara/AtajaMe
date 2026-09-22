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

        // 1. Liberar virtualmente modificadores que el usuario pudiera estar presionando (ej. Shift tras ':')
        ReleaseHeldModifiers();

        // 2. Si hay entorno WPF interactivo y el texto tiene contenido, usar pegado atómico
        // con preservación y restauración inmediata (garantiza inyección instantánea sin colisiones ni repeticiones)
        if (System.Windows.Application.Current != null && !string.IsNullOrEmpty(textToInject))
        {
            bool pasted = await InjectViaClipboardPreservingAsync(backspacesToDelete, textToInject, cancellationToken);
            if (pasted)
            {
                return true;
            }
        }

        // 3. Fallback / Entorno sin interfaz: SendInput directo
        return await InjectViaSendInputDirectAsync(backspacesToDelete, textToInject, cancellationToken);
    }

    private void ReleaseHeldModifiers()
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
            }
        }
        catch
        {
            // Ignorar fallos menores al liberar modificadores
        }
    }

    private async Task<bool> InjectViaClipboardPreservingAsync(
        int backspacesToDelete,
        string textToInject,
        CancellationToken cancellationToken)
    {
        string? originalClipboard = null;
        bool hadOriginalText = false;

        try
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (System.Windows.Clipboard.ContainsText())
                    {
                        originalClipboard = System.Windows.Clipboard.GetText();
                        hadOriginalText = true;
                    }
                    System.Windows.Clipboard.SetText(textToInject);
                }
                catch
                {
                    // Error de portapapeles ocupado
                }
            });
        }
        catch
        {
            return false;
        }

        int inputSize = Marshal.SizeOf<INPUT>();

        // 1. Borrar código disparador
        if (backspacesToDelete > 0)
        {
            var backInputs = new INPUT[backspacesToDelete * 2];
            int bIdx = 0;
            for (int i = 0; i < backspacesToDelete; i++)
            {
                backInputs[bIdx++] = CreateKeyInput(Win32Constants.VK_BACK, 0, 0);
                backInputs[bIdx++] = CreateKeyInput(Win32Constants.VK_BACK, 0, Win32Constants.KEYEVENTF_KEYUP);
            }

            _win32.SendInput((uint)backInputs.Length, backInputs, inputSize);
            await Task.Delay(25, cancellationToken);
        }

        // 2. Enviar Ctrl + V
        var pasteInputs = new INPUT[4];
        pasteInputs[0] = CreateKeyInput(Win32Constants.VK_CONTROL, 0, 0);
        pasteInputs[1] = CreateKeyInput((ushort)'V', 0, 0);
        pasteInputs[2] = CreateKeyInput((ushort)'V', 0, Win32Constants.KEYEVENTF_KEYUP);
        pasteInputs[3] = CreateKeyInput(Win32Constants.VK_CONTROL, 0, Win32Constants.KEYEVENTF_KEYUP);

        _win32.SendInput((uint)pasteInputs.Length, pasteInputs, inputSize);

        // 3. Esperar a que la app receptora procese el pegado
        await Task.Delay(40, cancellationToken);

        // 4. Restaurar de inmediato el portapapeles original del usuario (RF-06)
        try
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (hadOriginalText && originalClipboard != null)
                    {
                        System.Windows.Clipboard.SetText(originalClipboard);
                    }
                    else
                    {
                        System.Windows.Clipboard.Clear();
                    }
                }
                catch
                {
                    // Ignorar fallos de restauración
                }
            });
        }
        catch
        {
            // Ignorar
        }

        return true;
    }

    private async Task<bool> InjectViaSendInputDirectAsync(
        int backspacesToDelete,
        string textToInject,
        CancellationToken cancellationToken)
    {
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

        // 2. Inyección directa carácter a carácter
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
