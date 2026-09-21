using AtajaMe.Application.Expansion;
using AtajaMe.Application.Variables;
using AtajaMe.Domain.Entities;
using AtajaMe.Windows.Native;
using AtajaMe.Windows.Orchestration;

namespace AtajaMe.Tests.Windows;

public sealed class KeyboardExpansionOrchestratorTests
{
    private readonly FakeKeyboardHook _hook = new();
    private readonly FakeSnippetRepository _snippetRepo = new();
    private readonly VariableResolutionService _variableService = new();
    private readonly FakeTextInjector _textInjector = new();
    private readonly FakeSensitiveDetector _sensitiveDetector = new();
    private readonly FakeFocusTracker _focusTracker = new();
    private readonly FakeExcludedAppRepository _excludedAppsRepo = new();
    private readonly FakeWin32Api _fakeWin32 = new();

    private KeyboardExpansionOrchestrator CreateOrchestrator(SnippetExpansionEngine? engine = null)
    {
        engine ??= new SnippetExpansionEngine(_snippetRepo, _variableService);
        return new KeyboardExpansionOrchestrator(
            _hook,
            engine,
            _textInjector,
            _sensitiveDetector,
            _focusTracker,
            _excludedAppsRepo,
            _fakeWin32);
    }

    [Fact]
    public async Task HappyPath_WhenUserTypesSnippetCodeAndDelimiter_ExpandsAndInjectsText()
    {
        var snippet = Snippet.Create("bd", "Buenos días", "¡Buenos días!");
        await _snippetRepo.AddAsync(snippet);

        using var orchestrator = CreateOrchestrator();
        await orchestrator.StartAsync();

        // Emitir 'b', 'd', ' '
        _hook.EmitKey('b');
        _hook.EmitKey('d');
        _hook.EmitKey(' ');

        // Dar un breve margen al hilo consumidor en segundo plano
        await Task.Delay(100);

        Assert.Single(_textInjector.Injections);
        var injection = _textInjector.Injections[0];
        Assert.Equal(3, injection.Backspaces); // 'b' + 'd' + ' ' = 3
        Assert.Equal("¡Buenos días!", injection.Text);

        var updatedSnippet = await _snippetRepo.GetByIdAsync(snippet.Id);
        Assert.NotNull(updatedSnippet);
        Assert.Equal(1, updatedSnippet.UsageCount);
    }

    [Fact]
    public async Task WhenApplicationIsExcluded_ResetsBufferAndDoesNotInject()
    {
        var snippet = Snippet.Create("bd", "Buenos días", "¡Buenos días!");
        await _snippetRepo.AddAsync(snippet);

        // Excluir "notepad"
        await _excludedAppsRepo.AddAsync(ExcludedApplication.Create("notepad.exe"));
        _focusTracker.ActiveProcessName = "notepad";

        using var orchestrator = CreateOrchestrator();
        await orchestrator.StartAsync();

        _hook.EmitKey('b');
        _hook.EmitKey('d');
        _hook.EmitKey(' ');

        await Task.Delay(100);

        Assert.Empty(_textInjector.Injections);
    }

    [Fact]
    public async Task WhenSensitiveControlIsFocused_ResetsBufferAndDoesNotInject()
    {
        var snippet = Snippet.Create("bd", "Buenos días", "¡Buenos días!");
        await _snippetRepo.AddAsync(snippet);

        _sensitiveDetector.IsSensitive = true; // Campo de contraseña activo

        using var orchestrator = CreateOrchestrator();
        await orchestrator.StartAsync();

        _hook.EmitKey('b');
        _hook.EmitKey('d');
        _hook.EmitKey(' ');

        await Task.Delay(100);

        Assert.Empty(_textInjector.Injections);
    }

    [Fact]
    public async Task WhenFocusChangesBeforeInjection_AbortsAndDoesNotInject()
    {
        var snippet = Snippet.Create("bd", "Buenos días", "¡Buenos días!");
        await _snippetRepo.AddAsync(snippet);

        _focusTracker.FocusChangedReturnValue = true; // El foco cambió

        using var orchestrator = CreateOrchestrator();
        await orchestrator.StartAsync();

        _hook.EmitKey('b');
        _hook.EmitKey('d');
        _hook.EmitKey(' ');

        await Task.Delay(100);

        Assert.Empty(_textInjector.Injections);
    }

    [Fact]
    public async Task WhenNavigationKeyIsPressed_ResetsBuffer()
    {
        var snippet = Snippet.Create("bd", "Buenos días", "¡Buenos días!");
        await _snippetRepo.AddAsync(snippet);

        using var engine = new SnippetExpansionEngine(_snippetRepo, _variableService);
        using var orchestrator = CreateOrchestrator(engine);
        await orchestrator.StartAsync();

        _hook.EmitKey('b');
        await Task.Delay(50);
        Assert.Equal("b", engine.Buffer.GetContent());

        // Emitir flecha izquierda
        _hook.EmitVk(Win32Constants.VK_LEFT);
        await Task.Delay(50);

        Assert.Equal(string.Empty, engine.Buffer.GetContent());
    }

    [Fact]
    public async Task WhenSnippetRequiresInteractiveVariables_RaisesEventWithoutInjectingDirectly()
    {
        var snippet = Snippet.Create("cliente", "Saludo", "Hola {input:nombre}, un placer.");
        await _snippetRepo.AddAsync(snippet);

        using var orchestrator = CreateOrchestrator();
        await orchestrator.StartAsync();

        bool eventRaised = false;
        ExpansionResult? capturedResult = null;
        IntPtr capturedWindow = IntPtr.Zero;

        orchestrator.InteractiveInputRequested += (sender, args) =>
        {
            eventRaised = true;
            capturedResult = args.Result;
            capturedWindow = args.WindowHandle;
        };

        foreach (var c in "cliente ")
        {
            _hook.EmitKey(c);
        }

        await Task.Delay(100);

        Assert.True(eventRaised);
        Assert.NotNull(capturedResult);
        Assert.True(capturedResult.RequiresInput);
        Assert.Equal(new IntPtr(100), capturedWindow);
        Assert.Empty(_textInjector.Injections); // No inyectó todavía

        // Completar la expansión interactiva simulando la ventana flotante
        var userInputs = new Dictionary<string, string> { { "nombre", "Carlos" } };
        var completed = await orchestrator.CompleteInteractiveExpansionAsync(
            capturedResult,
            userInputs,
            capturedWindow);

        Assert.True(completed);
        Assert.Single(_textInjector.Injections);
        Assert.Equal("Hola Carlos, un placer.", _textInjector.Injections[0].Text);
    }

    [Fact]
    public async Task WhenOrchestratorIsPaused_IgnoresKeys()
    {
        var snippet = Snippet.Create("bd", "Buenos días", "¡Buenos días!");
        await _snippetRepo.AddAsync(snippet);

        using var orchestrator = CreateOrchestrator();
        orchestrator.IsPaused = true;
        await orchestrator.StartAsync();

        _hook.EmitKey('b');
        _hook.EmitKey('d');
        _hook.EmitKey(' ');

        await Task.Delay(100);

        Assert.Empty(_textInjector.Injections);
    }
}
