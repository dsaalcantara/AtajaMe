using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using AtajaMe.Application.Services;
using AtajaMe.Windows.Native;

namespace AtajaMe.Windows.Hooks;

/// <summary>
/// Implementación de bajo nivel del hook global de teclado WH_KEYBOARD_LL.
/// Encola los eventos en un canal asíncrono no bloqueante en menos de 0.5 ms,
/// rastrea el estado instantáneo de teclas modificadoras (Shift, AltGr, Control)
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

    // Estado físico inmediato de teclas modificadoras
    private volatile bool _isLShiftDown;
    private volatile bool _isRShiftDown;
    private volatile bool _isLControlDown;
    private volatile bool _isRControlDown;
    private volatile bool _isLMenuDown;
    private volatile bool _isRMenuDown;

    // Búferes reutilizables para evitar asignaciones de memoria en el callback del hook
    private readonly byte[] _keyboardState = new byte[256];
    private readonly StringBuilder _charBuffer = new(4);

    public ChannelReader<RawKeyEvent> KeyEvents => _channel.Reader;
    public bool IsRunning => _hookHandle != IntPtr.Zero;

    public LowLevelKeyboardHook(IWin32Api? win32 = null)
    {
        _win32 = win32 ?? NativeWin32Api.Instance;

        var options = new UnboundedChannelOptions
        {
            SingleWriter = true,
            SingleReader = false
        };
        _channel = Channel.CreateUnbounded<RawKeyEvent>(options);

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
                    return CallNextSafe(nCode, wParam, lParam);
                }

                int msg = (int)wParam;
                bool isKeyDown = msg == Win32Constants.WM_KEYDOWN || msg == Win32Constants.WM_SYSKEYDOWN;
                bool isKeyUp = msg == Win32Constants.WM_KEYUP || msg == Win32Constants.WM_SYSKEYUP;

                if (isKeyDown || isKeyUp)
                {
                    UpdateModifierState(kbdStruct.vkCode, isKeyDown);
                }

                // Solo procesar pulsaciones de tecla hacia abajo (KeyDown)
                if (isKeyDown)
                {
                    char? character = null;
                    if (!IsModifierKey((int)kbdStruct.vkCode))
                    {
                        character = TranslateKeyNative(kbdStruct.vkCode, kbdStruct.scanCode, kbdStruct.flags);
                    }

                    var rawEvent = new RawKeyEvent(
                        (int)kbdStruct.vkCode,
                        (int)kbdStruct.scanCode,
                        kbdStruct.flags,
                        kbdStruct.time,
                        character: character,
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

        return CallNextSafe(nCode, wParam, lParam);
    }

    private void UpdateModifierState(uint vkCode, bool isDown)
    {
        switch ((int)vkCode)
        {
            case Win32Constants.VK_SHIFT:
            case Win32Constants.VK_LSHIFT:
                _isLShiftDown = isDown;
                break;
            case Win32Constants.VK_RSHIFT:
                _isRShiftDown = isDown;
                break;
            case Win32Constants.VK_CONTROL:
            case Win32Constants.VK_LCONTROL:
                _isLControlDown = isDown;
                break;
            case Win32Constants.VK_RCONTROL:
                _isRControlDown = isDown;
                break;
            case Win32Constants.VK_MENU:
            case Win32Constants.VK_LMENU:
                _isLMenuDown = isDown;
                break;
            case Win32Constants.VK_RMENU:
                _isRMenuDown = isDown;
                if (!isDown)
                {
                    // Al soltar AltGr (VK_RMENU), limpiar inmediatamente cualquier residuo de LCONTROL sintetizado
                    _isLControlDown = false;
                }
                break;
        }
    }

    private char? TranslateKeyNative(uint vkCode, uint scanCode, uint flags)
    {
        try
        {
            switch ((int)vkCode)
            {
                case Win32Constants.VK_BACK: return '\b';
                case Win32Constants.VK_TAB: return '\t';
                case Win32Constants.VK_RETURN: return '\n';
                case Win32Constants.VK_SPACE: return ' ';
            }

            Array.Clear(_keyboardState, 0, _keyboardState.Length);
            var keyboardState = _keyboardState;
            _win32.GetKeyboardState(keyboardState);

            bool isShift = _isLShiftDown || _isRShiftDown || ((_win32.GetAsyncKeyState(Win32Constants.VK_SHIFT) & 0x8000) != 0);
            if (isShift)
            {
                keyboardState[Win32Constants.VK_SHIFT] = 0x80;
                if (_isLShiftDown) keyboardState[Win32Constants.VK_LSHIFT] = 0x80;
                if (_isRShiftDown) keyboardState[Win32Constants.VK_RSHIFT] = 0x80;
            }

            bool altDownFlag = (flags & 0x20) != 0; // LLKHF_ALTDOWN
            bool isRMenu = _isRMenuDown || ((_win32.GetAsyncKeyState(Win32Constants.VK_RMENU) & 0x8000) != 0);
            bool isLMenu = _isLMenuDown || ((_win32.GetAsyncKeyState(Win32Constants.VK_LMENU) & 0x8000) != 0);
            bool isMenu = isRMenu || isLMenu || altDownFlag || ((_win32.GetAsyncKeyState(Win32Constants.VK_MENU) & 0x8000) != 0);

            bool isLControl = _isLControlDown || ((_win32.GetAsyncKeyState(Win32Constants.VK_LCONTROL) & 0x8000) != 0);
            bool isRControl = _isRControlDown || ((_win32.GetAsyncKeyState(Win32Constants.VK_RCONTROL) & 0x8000) != 0);
            bool isControl = isLControl || isRControl || ((_win32.GetAsyncKeyState(Win32Constants.VK_CONTROL) & 0x8000) != 0);

            // AltGr en Windows: físicamente es VK_RMENU. AltGr equivale a Control + Menu simultáneos.
            bool isAltGr = isRMenu || (isControl && isMenu);

            if (isAltGr)
            {
                keyboardState[Win32Constants.VK_CONTROL] = 0x80;
                keyboardState[Win32Constants.VK_LCONTROL] = 0x80;
                keyboardState[Win32Constants.VK_MENU] = 0x80;
                keyboardState[Win32Constants.VK_RMENU] = 0x80;
            }
            else
            {
                if (isControl)
                {
                    keyboardState[Win32Constants.VK_CONTROL] = 0x80;
                    if (isLControl) keyboardState[Win32Constants.VK_LCONTROL] = 0x80;
                    if (isRControl) keyboardState[Win32Constants.VK_RCONTROL] = 0x80;
                }

                if (isMenu)
                {
                    keyboardState[Win32Constants.VK_MENU] = 0x80;
                    if (isLMenu) keyboardState[Win32Constants.VK_LMENU] = 0x80;
                    if (isRMenu) keyboardState[Win32Constants.VK_RMENU] = 0x80;
                }
            }

            if ((_win32.GetKeyState(Win32Constants.VK_CAPITAL) & 0x0001) != 0)
            {
                keyboardState[Win32Constants.VK_CAPITAL] = 0x01;
            }

            var sb = _charBuffer;
            sb.Clear();
            var hWnd = _win32.GetForegroundWindow();
            uint threadId = hWnd != IntPtr.Zero ? _win32.GetWindowThreadProcessId(hWnd, out _) : 0;
            var hkl = _win32.GetKeyboardLayout(threadId);

            int res = _win32.ToUnicodeEx(vkCode, scanCode, keyboardState, sb, sb.Capacity, 4, hkl);
            if (res == 0)
            {
                // Fallback sin flag 4 si la API nativa de Windows no traduce con flag 4
                res = _win32.ToUnicodeEx(vkCode, scanCode, keyboardState, sb, sb.Capacity, 0, hkl);
            }

            // Fallback crítico: si res == 0 y había Control marcado (sin AltGr), reintentar sin Control.
            // Esto recupera inmediatamente dígitos como 1 y 2 que Windows no traduce si hay residuo de Control tras soltar AltGr.
            if (res == 0 && keyboardState[Win32Constants.VK_CONTROL] != 0 && !isAltGr)
            {
                keyboardState[Win32Constants.VK_CONTROL] = 0;
                keyboardState[Win32Constants.VK_LCONTROL] = 0;
                keyboardState[Win32Constants.VK_RCONTROL] = 0;
                res = _win32.ToUnicodeEx(vkCode, scanCode, keyboardState, sb, sb.Capacity, 0, hkl);
            }

            if (res >= 1 && sb.Length > 0)
            {
                char ch = sb[sb.Length - 1];
                if (char.IsControl(ch) && ch != '\b' && ch != '\t' && ch != '\n' && ch != '\r')
                {
                    return null;
                }
                return ch;
            }
        }
        catch
        {
        }

        return null;
    }

    private static bool IsModifierKey(int vk)
    {
        return vk is Win32Constants.VK_SHIFT
            or Win32Constants.VK_LSHIFT
            or Win32Constants.VK_RSHIFT
            or Win32Constants.VK_CONTROL
            or Win32Constants.VK_LCONTROL
            or Win32Constants.VK_RCONTROL
            or Win32Constants.VK_MENU
            or Win32Constants.VK_LMENU
            or Win32Constants.VK_RMENU
            or Win32Constants.VK_CAPITAL
            or Win32Constants.VK_LWIN
            or Win32Constants.VK_RWIN;
    }

    private IntPtr CallNextSafe(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            return _win32.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }
        catch
        {
            return IntPtr.Zero;
        }
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
