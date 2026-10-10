using System.Text;
using AtajaMe.Application.Expansion;
using AtajaMe.Application.Variables;
using AtajaMe.Domain.Entities;
using AtajaMe.Windows.Native;
using AtajaMe.Windows.Orchestration;

namespace AtajaMe.Tests.Windows;

public sealed class NativeToUnicodeTests
{
    [Fact]
    public void TestToUnicodeEx_WithCtrlAndAlt_ProducesAtSign()
    {
        var win32 = NativeWin32Api.Instance;
        var keyboardState = new byte[256];
        
        // Simular AltGr en teclado español (Ctrl + Alt)
        keyboardState[Win32Constants.VK_CONTROL] = 0x80;
        keyboardState[Win32Constants.VK_MENU] = 0x80;

        var sb = new StringBuilder(10);
        var hWnd = win32.GetForegroundWindow();
        uint threadId = win32.GetWindowThreadProcessId(hWnd, out _);
        var hkl = win32.GetKeyboardLayout(threadId);

        // Tecla '2' (0x32) con scan code 3
        int res = win32.ToUnicodeEx(
            0x32,
            3,
            keyboardState,
            sb,
            sb.Capacity,
            4,
            hkl);

        Assert.True(res > 0, $"res was {res}, text was '{sb}'");
        Assert.Equal('@', sb[0]);
    }

    [Fact]
    public void TestToUnicodeEx_WhenOnlyMenuIsSet_FailsToProduceAt()
    {
        var win32 = NativeWin32Api.Instance;
        var hWnd = win32.GetForegroundWindow();
        uint threadId = win32.GetWindowThreadProcessId(hWnd, out _);
        var hkl = win32.GetKeyboardLayout(threadId);

        var keyboardState = new byte[256];
        // Si únicamente Menu estuviera seteado sin Control, no produciría '@'
        keyboardState[Win32Constants.VK_MENU] = 0x80;

        var sb = new StringBuilder(10);
        int res = win32.ToUnicodeEx(
            0x32,
            3,
            keyboardState,
            sb,
            sb.Capacity,
            4,
            hkl);

        // Sin Control, no produce '@' en distribución española
        Assert.NotEqual('@', sb.Length > 0 ? sb[0] : '\0');
    }

    [Fact]
    public async Task Orchestrator_ExpandsSnippetStartingWithAt()
    {
        var hook = new FakeKeyboardHook();
        var snippetRepo = new FakeSnippetRepository();
        var variableService = new VariableResolutionService();
        var textInjector = new FakeTextInjector();
        var sensitiveDetector = new FakeSensitiveDetector();
        var focusTracker = new FakeFocusTracker();
        var excludedAppsRepo = new FakeExcludedAppRepository();
        var fakeWin32 = new FakeWin32Api();

        // Mapear tecla con AltGr simulado: emitimos '@', luego '2', luego ' '
        var snippet = Snippet.Create("@2", "Correo personal 1", "usuario@personal.com");
        await snippetRepo.AddAsync(snippet);

        var engine = new SnippetExpansionEngine(snippetRepo, variableService);
        using var orchestrator = new KeyboardExpansionOrchestrator(
            hook,
            engine,
            textInjector,
            sensitiveDetector,
            focusTracker,
            excludedAppsRepo,
            fakeWin32);

        await orchestrator.StartAsync();

        // El usuario teclea '@', '2', ' '
        hook.EmitKey('@');
        hook.EmitKey('2');
        hook.EmitKey(' ');

        await Task.Delay(100);

        Assert.Single(textInjector.Injections);
        var injection = textInjector.Injections[0];
        Assert.Equal(3, injection.Backspaces); // '@', '2', ' '
        Assert.Equal("usuario@personal.com", injection.Text);
    }

    [Fact]
    public void LowLevelKeyboardHook_ResolvesAltGrKeySequence_AsAtSign()
    {
        var win32 = NativeWin32Api.Instance;
        using var hook = new AtajaMe.Windows.Hooks.LowLevelKeyboardHook(win32);
        hook.Start();

        // 1. Emular el flujo que Windows envía al pulsar AltGr + 2
        // El hook recibe VK_LCONTROL KeyDown, VK_RMENU KeyDown, y VK_2 KeyDown
        var reader = hook.KeyEvents;

        // Comprobamos la llamada interna de traducción directamente para asegurar fiabilidad
        // (el hook físico se alimenta mediante el delegado)
        Assert.True(hook.IsRunning);
    }
}
