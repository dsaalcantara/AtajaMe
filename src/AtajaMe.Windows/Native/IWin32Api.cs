using System.Text;

namespace AtajaMe.Windows.Native;

/// <summary>
/// Abstracción de llamadas nativas a la API Win32 de Windows.
/// Permite desacoplar el código de bajo nivel y facilitar pruebas unitarias reproducibles sin efectos colaterales.
/// </summary>
public interface IWin32Api
{
    IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    bool UnhookWindowsHookEx(IntPtr hhk);

    IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    IntPtr GetForegroundWindow();

    uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    bool GetKeyboardState(byte[] lpKeyState);

    int ToUnicodeEx(
        uint wVirtKey,
        uint wScanCode,
        byte[] lpKeyState,
        StringBuilder pwszBuff,
        int cchBuff,
        uint wFlags,
        IntPtr dwhkl);

    IntPtr GetKeyboardLayout(uint idThread);

    bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

    bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    bool GetCursorPos(out POINT lpPoint);

    short GetAsyncKeyState(int vKey);

    short GetKeyState(int vKey);
}
