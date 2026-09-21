using AtajaMe.Windows.Injection;
using AtajaMe.Windows.Native;

namespace AtajaMe.Tests.Windows;

public sealed class WindowsTextInjectorTests
{
    private readonly FakeWin32Api _fakeWin32 = new();

    [Fact]
    public async Task InjectAsync_WithZeroBackspacesAndEmptyText_ReturnsTrueWithoutCallingSendInput()
    {
        var injector = new WindowsTextInjector(_fakeWin32);

        var result = await injector.InjectAsync(0, string.Empty);

        Assert.True(result);
        Assert.Empty(_fakeWin32.SentInputs);
    }

    [Fact]
    public async Task InjectAsync_WithBackspacesAndText_SendsCorrectSequenceWithInjectedSignature()
    {
        var injector = new WindowsTextInjector(_fakeWin32);

        // Borrar 2 caracteres e inyectar "ab"
        var result = await injector.InjectAsync(2, "ab");

        Assert.True(result);
        Assert.Single(_fakeWin32.SentInputs);

        var inputs = _fakeWin32.SentInputs[0];
        // 2 retrocesos (2 * 2 = 4) + 2 caracteres (2 * 2 = 4) = 8 inputs en total
        Assert.Equal(8, inputs.Length);

        // Verificar los 2 retrocesos
        // 1. VK_BACK Down
        Assert.Equal(Win32Constants.INPUT_KEYBOARD, inputs[0].type);
        Assert.Equal(Win32Constants.VK_BACK, inputs[0].u.ki.wVk);
        Assert.Equal(0u, inputs[0].u.ki.dwFlags);
        Assert.Equal(Win32Constants.INJECTED_SIGNATURE, inputs[0].u.ki.dwExtraInfo);

        // 2. VK_BACK Up
        Assert.Equal(Win32Constants.VK_BACK, inputs[1].u.ki.wVk);
        Assert.Equal(Win32Constants.KEYEVENTF_KEYUP, inputs[1].u.ki.dwFlags);
        Assert.Equal(Win32Constants.INJECTED_SIGNATURE, inputs[1].u.ki.dwExtraInfo);

        // 3. VK_BACK Down
        Assert.Equal(Win32Constants.VK_BACK, inputs[2].u.ki.wVk);
        Assert.Equal(Win32Constants.INJECTED_SIGNATURE, inputs[2].u.ki.dwExtraInfo);

        // 4. VK_BACK Up
        Assert.Equal(Win32Constants.VK_BACK, inputs[3].u.ki.wVk);
        Assert.Equal(Win32Constants.KEYEVENTF_KEYUP, inputs[3].u.ki.dwFlags);

        // Verificar el primer carácter Unicode 'a'
        // 5. 'a' Down
        Assert.Equal(0, inputs[4].u.ki.wVk);
        Assert.Equal((ushort)'a', inputs[4].u.ki.wScan);
        Assert.Equal(Win32Constants.KEYEVENTF_UNICODE, inputs[4].u.ki.dwFlags);
        Assert.Equal(Win32Constants.INJECTED_SIGNATURE, inputs[4].u.ki.dwExtraInfo);

        // 6. 'a' Up
        Assert.Equal(0, inputs[5].u.ki.wVk);
        Assert.Equal((ushort)'a', inputs[5].u.ki.wScan);
        Assert.Equal(Win32Constants.KEYEVENTF_UNICODE | Win32Constants.KEYEVENTF_KEYUP, inputs[5].u.ki.dwFlags);

        // Verificar el segundo carácter Unicode 'b'
        // 7. 'b' Down
        Assert.Equal((ushort)'b', inputs[6].u.ki.wScan);
        Assert.Equal(Win32Constants.KEYEVENTF_UNICODE, inputs[6].u.ki.dwFlags);

        // 8. 'b' Up
        Assert.Equal((ushort)'b', inputs[7].u.ki.wScan);
        Assert.Equal(Win32Constants.KEYEVENTF_UNICODE | Win32Constants.KEYEVENTF_KEYUP, inputs[7].u.ki.dwFlags);
    }

    [Fact]
    public async Task InjectAsync_SupportsSpecialCharacters_LikeAccentsAndNewlines()
    {
        var injector = new WindowsTextInjector(_fakeWin32);

        var result = await injector.InjectAsync(0, "ñá\n");

        Assert.True(result);
        var inputs = _fakeWin32.SentInputs[0];
        Assert.Equal(6, inputs.Length); // 3 caracteres * 2

        Assert.Equal((ushort)'ñ', inputs[0].u.ki.wScan);
        Assert.Equal((ushort)'á', inputs[2].u.ki.wScan);
        Assert.Equal((ushort)'\n', inputs[4].u.ki.wScan);
    }

    [Fact]
    public async Task InjectAsync_WhenCancelled_ReturnsFalseImmediately()
    {
        var injector = new WindowsTextInjector(_fakeWin32);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await injector.InjectAsync(5, "texto", cts.Token);

        Assert.False(result);
        Assert.Empty(_fakeWin32.SentInputs);
    }
}
