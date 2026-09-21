using AtajaMe.Windows.Hooks;
using AtajaMe.Windows.Native;

namespace AtajaMe.Tests.Windows;

public sealed class LowLevelKeyboardHookTests
{
    private readonly FakeWin32Api _fakeWin32 = new();

    [Fact]
    public void Start_InstallsHook_WithLowLevelKeyboardId()
    {
        using var hook = new LowLevelKeyboardHook(_fakeWin32);

        hook.Start();

        Assert.True(hook.IsRunning);
        Assert.Equal(Win32Constants.WH_KEYBOARD_LL, _fakeWin32.InstalledHookId);
        Assert.NotNull(_fakeWin32.InstalledHookProc);
    }

    [Fact]
    public void Stop_UninstallsHook_Correctly()
    {
        using var hook = new LowLevelKeyboardHook(_fakeWin32);
        hook.Start();

        hook.Stop();

        Assert.False(hook.IsRunning);
        Assert.True(_fakeWin32.UnhookCalled);
    }

    [Fact]
    public void HookCallback_WhenUserPressesKey_EnqueuesRawKeyEvent()
    {
        using var hook = new LowLevelKeyboardHook(_fakeWin32);
        hook.Start();

        _fakeWin32.SimulateHookCallback(
            vkCode: 0x41, // 'A'
            scanCode: 30,
            flags: 0,
            dwExtraInfo: UIntPtr.Zero,
            isKeyDown: true);

        var reader = hook.KeyEvents;
        Assert.True(reader.TryRead(out var keyEvent));
        Assert.NotNull(keyEvent);
        Assert.Equal(0x41, keyEvent.VirtualKeyCode);
        Assert.Equal(30, keyEvent.ScanCode);
        Assert.True(keyEvent.IsKeyDown);
    }

    [Fact]
    public void HookCallback_WhenKeyUp_DoesNotEnqueueEvent()
    {
        using var hook = new LowLevelKeyboardHook(_fakeWin32);
        hook.Start();

        _fakeWin32.SimulateHookCallback(
            vkCode: 0x41,
            scanCode: 30,
            flags: 0,
            dwExtraInfo: UIntPtr.Zero,
            isKeyDown: false); // KeyUp

        var reader = hook.KeyEvents;
        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public void HookCallback_WhenInjectedByAtajaMe_IgnoresAndDoesNotEnqueue()
    {
        using var hook = new LowLevelKeyboardHook(_fakeWin32);
        hook.Start();

        // Pulsación marcada con la firma INJECTED_SIGNATURE
        _fakeWin32.SimulateHookCallback(
            vkCode: Win32Constants.VK_BACK,
            scanCode: 14,
            flags: 0,
            dwExtraInfo: Win32Constants.INJECTED_SIGNATURE,
            isKeyDown: true);

        var reader = hook.KeyEvents;
        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public void Dispose_StopsHookAndCompletesChannel()
    {
        var hook = new LowLevelKeyboardHook(_fakeWin32);
        hook.Start();

        hook.Dispose();

        Assert.False(hook.IsRunning);
        Assert.True(_fakeWin32.UnhookCalled);
        Assert.True(hook.KeyEvents.Completion.IsCompleted);
    }
}
