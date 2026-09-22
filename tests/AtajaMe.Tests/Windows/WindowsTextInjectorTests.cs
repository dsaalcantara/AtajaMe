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
        // Debe realizarse en dos etapas: una para los retrocesos y otra para el texto inyectado
        Assert.Equal(2, _fakeWin32.SentInputs.Count);

        var backInputs = _fakeWin32.SentInputs[0];
        Assert.Equal(4, backInputs.Length); // 2 retrocesos * 2 (down y up)

        // Verificar los 2 retrocesos
        // 1. VK_BACK Down
        Assert.Equal(Win32Constants.INPUT_KEYBOARD, backInputs[0].type);
        Assert.Equal(Win32Constants.VK_BACK, backInputs[0].u.ki.wVk);
        Assert.Equal(0u, backInputs[0].u.ki.dwFlags);
        Assert.Equal(Win32Constants.INJECTED_SIGNATURE, backInputs[0].u.ki.dwExtraInfo);

        // 2. VK_BACK Up
        Assert.Equal(Win32Constants.VK_BACK, backInputs[1].u.ki.wVk);
        Assert.Equal(Win32Constants.KEYEVENTF_KEYUP, backInputs[1].u.ki.dwFlags);

        var textInputs = _fakeWin32.SentInputs[1];
        Assert.Equal(4, textInputs.Length); // 2 caracteres * 2 (down y up)

        // Verificar el primer carácter Unicode 'a'
        Assert.Equal(0, textInputs[0].u.ki.wVk);
        Assert.Equal((ushort)'a', textInputs[0].u.ki.wScan);
        Assert.Equal(Win32Constants.KEYEVENTF_UNICODE, textInputs[0].u.ki.dwFlags);
        Assert.Equal(Win32Constants.INJECTED_SIGNATURE, textInputs[0].u.ki.dwExtraInfo);

        Assert.Equal(0, textInputs[1].u.ki.wVk);
        Assert.Equal((ushort)'a', textInputs[1].u.ki.wScan);
        Assert.Equal(Win32Constants.KEYEVENTF_UNICODE | Win32Constants.KEYEVENTF_KEYUP, textInputs[1].u.ki.dwFlags);

        // Verificar el segundo carácter Unicode 'b'
        Assert.Equal((ushort)'b', textInputs[2].u.ki.wScan);
        Assert.Equal(Win32Constants.KEYEVENTF_UNICODE, textInputs[2].u.ki.dwFlags);

        Assert.Equal((ushort)'b', textInputs[3].u.ki.wScan);
        Assert.Equal(Win32Constants.KEYEVENTF_UNICODE | Win32Constants.KEYEVENTF_KEYUP, textInputs[3].u.ki.dwFlags);
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
        Assert.Equal(Win32Constants.VK_RETURN, inputs[4].u.ki.wVk);
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


    [Fact]
    public void InputStruct_In64BitProcess_HasCorrectSizeAndOffset()
    {
        int inputSize = System.Runtime.InteropServices.Marshal.SizeOf<INPUT>();
        int offsetOfU = (int)System.Runtime.InteropServices.Marshal.OffsetOf<INPUT>("u");
        if (IntPtr.Size == 8)
        {
            Assert.Equal(40, inputSize);
            Assert.Equal(8, offsetOfU);
        }
        else
        {
            Assert.Equal(28, inputSize);
            Assert.Equal(4, offsetOfU);
        }
    }
}
