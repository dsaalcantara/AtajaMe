using AtajaMe.Windows.Focus;
using AtajaMe.Windows.Native;

namespace AtajaMe.Tests.Windows;

public sealed class WindowsCaretLocatorTests
{
    [Fact]
    public void GetCaretOrCursorPosition_WhenCaretAvailable_ReturnsCaretCoordinates()
    {
        // Arrange
        var fakeWin32 = new FakeWin32Api
        {
            ForegroundProcessId = 4321,
            GuiThreadInfoSuccess = true,
            GuiThreadInfoToReturn = new GUITHREADINFO
            {
                hwndCaret = new IntPtr(888),
                rcCaret = new RECT { Left = 250, Top = 100, Right = 255, Bottom = 120 }
            },
            ClientToScreenSuccess = true,
            ClientToScreenPointToReturn = new POINT(250, 120)
        };

        var locator = new WindowsCaretLocator(fakeWin32);

        // Act
        var (x, y) = locator.GetCaretOrCursorPosition(new IntPtr(999));

        // Assert
        Assert.Equal(250, x);
        Assert.Equal(124, y); // Bottom + 4
    }

    [Fact]
    public void GetCaretOrCursorPosition_WhenCaretNotAvailable_FallsBackToMouseCursor()
    {
        // Arrange
        var fakeWin32 = new FakeWin32Api
        {
            GuiThreadInfoSuccess = false,
            CursorPosToReturn = new POINT(450, 300)
        };

        var locator = new WindowsCaretLocator(fakeWin32);

        // Act
        var (x, y) = locator.GetCaretOrCursorPosition(new IntPtr(999));

        // Assert
        Assert.Equal(450, x);
        Assert.Equal(316, y); // Mouse Y + 16
    }

    [Fact]
    public void GetCaretOrCursorPosition_WhenWindowHandleIsZero_FallsBackToMouseCursor()
    {
        // Arrange
        var fakeWin32 = new FakeWin32Api
        {
            CursorPosToReturn = new POINT(500, 600)
        };

        var locator = new WindowsCaretLocator(fakeWin32);

        // Act
        var (x, y) = locator.GetCaretOrCursorPosition(IntPtr.Zero);

        // Assert
        Assert.Equal(500, x);
        Assert.Equal(616, y);
    }
}
