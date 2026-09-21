using System.Runtime.InteropServices;
using System.Threading.Channels;
using AtajaMe.Application.Services;
using AtajaMe.Windows.Native;

namespace AtajaMe.Windows.Hooks;

/// <summary>
/// Implementación de bajo nivel del hook global de teclado WH_KEYBOARD_LL.
/// Encola los eventos en un canal asíncrono no bloqueante en menos de 0.5 ms
/// y mantiene el delegado anclado contra el recolector de basura (GC).
/// </summary>
public sealed class LowLevelKeyboardHook : IKeyboardHook
{
    private readonly IWin32Api _win32;
    private readonly Channel<RawKeyEvent> _channel;
    private readonly HookProc _hookProc;
    private IntPtr _hookHandle = IntPtr.Zero;
    private readonly object _syncLock = new();
    private bool _disposed;

    public ChannelReader<RawKeyEvent> KeyEvents => _channel.Reader;
    public bool IsRunning => _hookHandle != IntPtr.Zero;

    public LowLevelKeyboardHook(IWin32Api? win32 = null)
    {
        _win32 = win32 ?? NativeWin32Api.Instance;

        // Canal asíncrono para desacoplar el procesamiento del hilo del sistema operativo
        var options = new UnboundedChannelOptions
        {
            SingleWriter = true,
            SingleReader = false
        };
        _channel = Channel.CreateUnbounded<RawKeyEvent>(options);

        // Mantener referencia persistente al delegado para evitar CallbackOnCollectedDelegate
        _hookProc = HookCallback;
    }

    public void Start()
    {
        lock (_syncLock)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LowLevelKeyboardHook));
            }

            if (_hookHandle != IntPtr.Zero)
            {
                return;
            }

            _hookHandle = _win32.SetWindowsHookEx(
                Win32Constants.WH_KEYBOARD_LL,
                _hookProc,
                IntPtr.Zero,
                0);

            if (_hookHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("No se pudo instalar el hook de teclado de bajo nivel.");
            }
        }
    }

    public void Stop()
    {
        lock (_syncLock)
        {
            if (_hookHandle != IntPtr.Zero)
            {
                _win32.UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && lParam != IntPtr.Zero)
        {
            try
            {
                var kbdStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                // Descartar inmediatamente eventos generados por nuestra propia inyección
                if (kbdStruct.dwExtraInfo == Win32Constants.INJECTED_SIGNATURE)
                {
                    return _win32.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
                }

                int msg = (int)wParam;
                bool isKeyDown = msg == Win32Constants.WM_KEYDOWN || msg == Win32Constants.WM_SYSKEYDOWN;

                // Solo procesar pulsaciones de tecla hacia abajo (KeyDown)
                if (isKeyDown)
                {
                    var rawEvent = new RawKeyEvent(
                        (int)kbdStruct.vkCode,
                        (int)kbdStruct.scanCode,
                        kbdStruct.flags,
                        kbdStruct.time,
                        character: null,
                        isKeyDown: true);

                    // Encolado no bloqueante inmediato (< 0.1 ms)
                    _channel.Writer.TryWrite(rawEvent);
                }
            }
            catch
            {
                // El callback jamás debe lanzar excepciones al subsistema de ventanas
            }
        }

        return _win32.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        lock (_syncLock)
        {
            if (_disposed)
            {
                return;
            }

            Stop();
            _channel.Writer.TryComplete();
            _disposed = true;
            GC.KeepAlive(_hookProc);
        }
    }
}
