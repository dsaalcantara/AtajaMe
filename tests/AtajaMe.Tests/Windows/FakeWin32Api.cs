using System.Text;
using AtajaMe.Windows.Native;

namespace AtajaMe.Tests.Windows;

public sealed class FakeWin32Api : IWin32Api
{
    public HookProc? InstalledHookProc { get; private set; }
    public int InstalledHookId { get; private set; }
    public IntPtr HookHandleToReturn { get; set; } = new IntPtr(12345);
    public bool UnhookCalled { get; private set; }

    public List<INPUT[]> SentInputs { get; } = new();
    public uint SendInputReturnValue { get; set; }

    public IntPtr ForegroundWindowHandle { get; set; } = new IntPtr(999);
    public uint ForegroundProcessId { get; set; } = 1000;

    public byte[] KeyboardState { get; set; } = new byte[256];
    public Dictionary<uint, char> KeyToUnicodeMap { get; } = new();

    public IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId)
    {
        InstalledHookId = idHook;
        InstalledHookProc = lpfn;
        return HookHandleToReturn;
    }

    public bool UnhookWindowsHookEx(IntPtr hhk)
    {
        UnhookCalled = true;
        return true;
    }

    public IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam)
    {
        return IntPtr.Zero;
    }

    public uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize)
    {
        SentInputs.Add(pInputs);
        return SendInputReturnValue > 0 ? SendInputReturnValue : nInputs;
    }

    public IntPtr GetForegroundWindow()
    {
        return ForegroundWindowHandle;
    }

    public uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId)
    {
        lpdwProcessId = ForegroundProcessId;
        return 1;
    }

    public bool GetKeyboardState(byte[] lpKeyState)
    {
        Array.Copy(KeyboardState, lpKeyState, Math.Min(KeyboardState.Length, lpKeyState.Length));
        return true;
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
        if (KeyToUnicodeMap.TryGetValue(wVirtKey, out char mappedChar))
        {
            pwszBuff.Append(mappedChar);
            return 1;
        }

        return 0;
    }

    public IntPtr GetKeyboardLayout(uint idThread)
    {
        return IntPtr.Zero;
    }

    public bool GuiThreadInfoSuccess { get; set; } = false;
    public GUITHREADINFO GuiThreadInfoToReturn { get; set; }
    public bool ClientToScreenSuccess { get; set; } = false;
    public POINT ClientToScreenPointToReturn { get; set; }
    public POINT CursorPosToReturn { get; set; } = new POINT(100, 100);

    public bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui)
    {
        if (GuiThreadInfoSuccess)
        {
            lpgui = GuiThreadInfoToReturn;
            return true;
        }
        return false;
    }

    public bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint)
    {
        if (ClientToScreenSuccess)
        {
            lpPoint = ClientToScreenPointToReturn;
            return true;
        }
        return false;
    }

    public bool GetCursorPos(out POINT lpPoint)
    {
        lpPoint = CursorPosToReturn;
        return true;
    }

    public HashSet<int> PressedKeys { get; } = new();

    public short GetAsyncKeyState(int vKey) => PressedKeys.Contains(vKey) ? unchecked((short)0x8000) : (short)0;

    public short GetKeyState(int vKey) => 0;

    /// <summary>
    /// Simula un evento de hook invocando el delegado registrado con una estructura KBDLLHOOKSTRUCT.
    /// </summary>
    public IntPtr SimulateHookCallback(int vkCode, int scanCode, uint flags, UIntPtr dwExtraInfo, bool isKeyDown = true)
    {
        if (InstalledHookProc == null)
        {
            throw new InvalidOperationException("No se ha instalado ningún hook delegado.");
        }

        var kbd = new KBDLLHOOKSTRUCT
        {
            vkCode = (uint)vkCode,
            scanCode = (uint)scanCode,
            flags = flags,
            time = 100,
            dwExtraInfo = dwExtraInfo
        };

        var ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(System.Runtime.InteropServices.Marshal.SizeOf(kbd));
        try
        {
            System.Runtime.InteropServices.Marshal.StructureToPtr(kbd, ptr, false);
            IntPtr wParam = isKeyDown ? (IntPtr)Win32Constants.WM_KEYDOWN : (IntPtr)Win32Constants.WM_KEYUP;
            return InstalledHookProc(0, wParam, ptr);
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);
        }
    }
}
