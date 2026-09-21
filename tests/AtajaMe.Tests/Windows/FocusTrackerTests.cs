using AtajaMe.Windows.Focus;

namespace AtajaMe.Tests.Windows;

public sealed class FocusTrackerTests
{
    private readonly FakeWin32Api _fakeWin32 = new();

    [Fact]
    public void GetActiveWindowHandle_ReturnsCurrentForegroundWindow()
    {
        var expectedHandle = new IntPtr(777);
        _fakeWin32.ForegroundWindowHandle = expectedHandle;
        var tracker = new FocusTracker(_fakeWin32);

        var handle = tracker.GetActiveWindowHandle();

        Assert.Equal(expectedHandle, handle);
    }

    [Fact]
    public void HasFocusChanged_WhenHandleIsSame_ReturnsFalse()
    {
        var windowHandle = new IntPtr(555);
        _fakeWin32.ForegroundWindowHandle = windowHandle;
        var tracker = new FocusTracker(_fakeWin32);

        var changed = tracker.HasFocusChanged(windowHandle);

        Assert.False(changed);
    }

    [Fact]
    public void HasFocusChanged_WhenHandleIsDifferent_ReturnsTrue()
    {
        _fakeWin32.ForegroundWindowHandle = new IntPtr(555);
        var tracker = new FocusTracker(_fakeWin32);

        var changed = tracker.HasFocusChanged(new IntPtr(999));

        Assert.True(changed);
    }

    [Fact]
    public void HasFocusChanged_WhenOriginalHandleIsZero_ReturnsTrue()
    {
        var tracker = new FocusTracker(_fakeWin32);

        var changed = tracker.HasFocusChanged(IntPtr.Zero);

        Assert.True(changed);
    }
}
