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
    public void HasFocusChanged_WhenHandleIsDifferentAndDifferentProcess_ReturnsTrue()
    {
        _fakeWin32.ForegroundWindowHandle = new IntPtr(555);
        _fakeWin32.WindowProcessMap[new IntPtr(555)] = 1001;
        _fakeWin32.WindowProcessMap[new IntPtr(999)] = 1002;
        var tracker = new FocusTracker(_fakeWin32);

        var changed = tracker.HasFocusChanged(new IntPtr(999));

        Assert.True(changed);
    }

    [Fact]
    public void HasFocusChanged_WhenHandleIsDifferentButSameProcess_ReturnsFalse()
    {
        _fakeWin32.ForegroundWindowHandle = new IntPtr(555);
        _fakeWin32.WindowProcessMap[new IntPtr(555)] = 1000;
        _fakeWin32.WindowProcessMap[new IntPtr(999)] = 1000;
        var tracker = new FocusTracker(_fakeWin32);

        var changed = tracker.HasFocusChanged(new IntPtr(999));

        Assert.False(changed);
    }

    [Fact]
    public void HasFocusChanged_WhenOriginalHandleIsZero_ReturnsTrue()
    {
        var tracker = new FocusTracker(_fakeWin32);

        var changed = tracker.HasFocusChanged(IntPtr.Zero);

        Assert.True(changed);
    }

    [Fact]
    public void IsCurrentAppActive_WhenForegroundProcessMatchesCurrentProcess_ReturnsTrue()
    {
        _fakeWin32.ForegroundWindowHandle = new IntPtr(123);
        _fakeWin32.ForegroundProcessId = (uint)Environment.ProcessId;
        var tracker = new FocusTracker(_fakeWin32);

        var isActive = tracker.IsCurrentAppActive();

        Assert.True(isActive);
    }

    [Fact]
    public void IsCurrentAppActive_WhenForegroundProcessIsDifferent_ReturnsFalse()
    {
        _fakeWin32.ForegroundWindowHandle = new IntPtr(123);
        _fakeWin32.ForegroundProcessId = (uint)Environment.ProcessId + 9999;
        var tracker = new FocusTracker(_fakeWin32);

        var isActive = tracker.IsCurrentAppActive();

        Assert.False(isActive);
    }

    [Fact]
    public void IsCurrentAppActive_WhenForegroundWindowIsZero_ReturnsFalse()
    {
        _fakeWin32.ForegroundWindowHandle = IntPtr.Zero;
        var tracker = new FocusTracker(_fakeWin32);

        var isActive = tracker.IsCurrentAppActive();

        Assert.False(isActive);
    }
}
