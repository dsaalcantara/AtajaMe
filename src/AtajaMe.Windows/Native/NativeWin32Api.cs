using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace AtajaMe.Windows.Native;

/// <summary>
/// Implementación nativa de IWin32Api mediante P/Invoke sobre user32.dll y kernel32.dll.
/// </summary>
public sealed class NativeWin32Api : IWin32Api
{
    private static readonly Lazy<NativeWin32Api> _instance = new(() => new NativeWin32Api());
    public static NativeWin32Api Instance => _instance.Value;

    public IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId)
    {
        if (hMod == IntPtr.Zero)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            if (curModule != null)
            {
                hMod = GetModuleHandle(curModule.ModuleName);
            }
        }

        return SetWindowsHookExW(idHook, lpfn, hMod, dwThreadId);
    }

    public bool UnhookWindowsHookEx(IntPtr hhk)
    {
        return UnhookWindowsHookExNative(hhk);
    }

    public IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam)
    {
        return CallNextHookExNative(hhk, nCode, wParam, lParam);
    }

    public uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize)
    {
        return SendInputNative(nInputs, pInputs, cbSize);
    }

    public IntPtr GetForegroundWindow()
    {
        return GetForegroundWindowNative();
    }

    public uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId)
    {
        return GetWindowThreadProcessIdNative(hWnd, out lpdwProcessId);
    }

    public bool GetKeyboardState(byte[] lpKeyState)
    {
        return GetKeyboardStateNative(lpKeyState);
    }

    public int ToUnicodeEx(
        uint wVirtKey,
        uint wScanCode,
        byte[] lpKeyState,
        StringBuilder pwszBuff,
        int cchBuff,
        uint wFlags,
        IntPtr dwhkl)
    {
        return ToUnicodeExNative(wVirtKey, wScanCode, lpKeyState, pwszBuff, cchBuff, wFlags, dwhkl);
    }

    public IntPtr GetKeyboardLayout(uint idThread)
    {
        return GetKeyboardLayoutNative(idThread);
    }

    public bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui)
    {
        return GetGUIThreadInfoNative(idThread, ref lpgui);
    }

    public bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint)
    {
        return ClientToScreenNative(hWnd, ref lpPoint);
    }

    public bool GetCursorPos(out POINT lpPoint)
    {
        return GetCursorPosNative(out lpPoint);
    }

    public short GetAsyncKeyState(int vKey)
    {
        return GetAsyncKeyStateNative(vKey);
    }

    public short GetKeyState(int vKey)
    {
        return GetKeyStateNative(vKey);
    }

    #region P/Invoke Definitions

    [DllImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SetWindowsHookExW(
        int idHook,
        HookProc lpfn,
        IntPtr hMod,
        uint dwThreadId);

    [DllImport("user32.dll", EntryPoint = "UnhookWindowsHookEx", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookExNative(IntPtr hhk);

    [DllImport("user32.dll", EntryPoint = "CallNextHookEx")]
    private static extern IntPtr CallNextHookExNative(
        IntPtr hhk,
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "SendInput", SetLastError = true)]
    private static extern uint SendInputNative(
        uint nInputs,
        [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs,
        int cbSize);

    [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
    private static extern IntPtr GetForegroundWindowNative();

    [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", SetLastError = true)]
    private static extern uint GetWindowThreadProcessIdNative(
        IntPtr hWnd,
        out uint lpdwProcessId);

    [DllImport("user32.dll", EntryPoint = "GetKeyboardState")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetKeyboardStateNative([Out] byte[] lpKeyState);

    [DllImport("user32.dll", EntryPoint = "ToUnicodeEx", CharSet = CharSet.Unicode)]
    private static extern int ToUnicodeExNative(
        uint wVirtKey,
        uint wScanCode,
        [In] byte[] lpKeyState,
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff,
        int cchBuff,
        uint wFlags,
        IntPtr dwhkl);

    [DllImport("user32.dll", EntryPoint = "GetKeyboardLayout")]
    private static extern IntPtr GetKeyboardLayoutNative(uint idThread);

    [DllImport("user32.dll", EntryPoint = "GetGUIThreadInfo", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetGUIThreadInfoNative(uint idThread, ref GUITHREADINFO lpgui);

    [DllImport("user32.dll", EntryPoint = "ClientToScreen")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreenNative(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll", EntryPoint = "GetCursorPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPosNative(out POINT lpPoint);

    [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll", EntryPoint = "GetAsyncKeyState")]
    private static extern short GetAsyncKeyStateNative(int vKey);

    [DllImport("user32.dll", EntryPoint = "GetKeyState")]
    private static extern short GetKeyStateNative(int vKey);

    #endregion
}
