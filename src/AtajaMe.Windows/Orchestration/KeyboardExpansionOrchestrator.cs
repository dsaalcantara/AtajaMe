using System.Text;
using AtajaMe.Application.Expansion;
using AtajaMe.Application.Services;
using AtajaMe.Domain.Repositories;
using AtajaMe.Windows.Native;

namespace AtajaMe.Windows.Orchestration;

/// <summary>
/// Orquestador central en segundo plano que consume pulsaciones de teclado del hook,
/// verifica políticas de seguridad y exclusión, interactúa con el motor de expansión
/// y ejecuta la inyección de texto sin tocar el portapapeles.
/// </summary>
public sealed class KeyboardExpansionOrchestrator : IKeyboardExpansionOrchestrator
{
    private readonly IKeyboardHook _hook;
    private readonly ISnippetExpansionEngine _engine;
    private readonly ITextInjector _textInjector;
    private readonly ISensitiveControlDetector _sensitiveDetector;
    private readonly IFocusTracker _focusTracker;
    private readonly IExcludedApplicationRepository _excludedAppsRepository;
    private readonly IWin32Api _win32;

    private readonly object _syncLock = new();
    private CancellationTokenSource? _cts;
    private Task? _consumerTask;
    private bool _disposed;

    public event EventHandler<InteractiveInputRequestedEventArgs>? InteractiveInputRequested;

    public bool IsRunning => _consumerTask != null && !_consumerTask.IsCompleted;
    public bool IsPaused { get; set; }

    public KeyboardExpansionOrchestrator(
        IKeyboardHook hook,
        ISnippetExpansionEngine engine,
        ITextInjector textInjector,
        ISensitiveControlDetector sensitiveDetector,
        IFocusTracker focusTracker,
        IExcludedApplicationRepository excludedAppsRepository,
        IWin32Api? win32 = null)
    {
        _hook = hook ?? throw new ArgumentNullException(nameof(hook));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _textInjector = textInjector ?? throw new ArgumentNullException(nameof(textInjector));
        _sensitiveDetector = sensitiveDetector ?? throw new ArgumentNullException(nameof(sensitiveDetector));
        _focusTracker = focusTracker ?? throw new ArgumentNullException(nameof(focusTracker));
        _excludedAppsRepository = excludedAppsRepository ?? throw new ArgumentNullException(nameof(excludedAppsRepository));
        _win32 = win32 ?? NativeWin32Api.Instance;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncLock)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(KeyboardExpansionOrchestrator));
            }

            if (IsRunning)
            {
                return Task.CompletedTask;
            }

            _cts = new CancellationTokenSource();
            _hook.Start();
            _consumerTask = Task.Run(() => ConsumeKeyEventsAsync(_cts.Token));
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        Task? taskToWait = null;

        lock (_syncLock)
        {
            if (!IsRunning)
            {
                return;
            }

            _cts?.Cancel();
            _hook.Stop();
            taskToWait = _consumerTask;
        }

        if (taskToWait != null)
        {
            try
            {
                await taskToWait.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada
            }
        }
    }

    private async Task ConsumeKeyEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _hook.KeyEvents.WaitToReadAsync(cancellationToken))
            {
                while (_hook.KeyEvents.TryRead(out var keyEvent))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (IsPaused)
                    {
                        continue;
                    }

                    await ProcessRawKeyEventAsync(keyEvent, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Detención limpia
        }
        catch
        {
            // Evitar que excepciones imprevistas cierren el proceso
        }
    }

    private async Task ProcessRawKeyEventAsync(RawKeyEvent keyEvent, CancellationToken cancellationToken)
    {
        if (IsNavigationOrResetKey(keyEvent.VirtualKeyCode))
        {
            _engine.ResetBuffer();
            return;
        }

        // Si la ventana activa pertenece a AtajaMe, no capturar ni expandir nada en la propia aplicación
        if (_focusTracker.IsCurrentAppActive())
        {
            _engine.ResetBuffer();
            return;
        }

        char? character = TranslateKeyToChar(keyEvent);
        if (!character.HasValue)
        {
            return;
        }

        var activeWindow = _focusTracker.GetActiveWindowHandle();

        var result = await _engine.ProcessKeyAsync(character.Value, cancellationToken);
        if (!result.IsMatch)
        {
            return;
        }

        // Antes de expandir cualquier atajo, verificar que el foco no haya cambiado y que el objetivo sea seguro (RF-08)
        if ((activeWindow != IntPtr.Zero && _focusTracker.HasFocusChanged(activeWindow)) || !await IsExecutionTargetAllowedAsync(cancellationToken))
        {
            _engine.ResetBuffer();
            return;
        }

        if (result.RequiresInput)
        {
            InteractiveInputRequested?.Invoke(this, new InteractiveInputRequestedEventArgs(result, activeWindow));
            return;
        }

        await ExecuteDirectInjectionAsync(result, cancellationToken);
    }

    private async Task<bool> IsExecutionTargetAllowedAsync(CancellationToken cancellationToken)
    {
        if (_focusTracker.IsCurrentAppActive())
        {
            return false;
        }

        var processName = _focusTracker.GetActiveProcessName();
        if (!string.IsNullOrEmpty(processName))
        {
            bool isExcluded = await _excludedAppsRepository.IsExcludedAsync(processName, cancellationToken);
            if (!isExcluded && !processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                isExcluded = await _excludedAppsRepository.IsExcludedAsync($"{processName}.exe", cancellationToken);
            }

            if (isExcluded)
            {
                return false;
            }
        }

        return !_sensitiveDetector.IsSensitiveControlFocused();
    }

    private async Task ExecuteDirectInjectionAsync(ExpansionResult result, CancellationToken cancellationToken)
    {
        if (result.Status == ExpansionStatus.Success && result.ExpandedContent != null)
        {
            IsPaused = true;
            try
            {
                bool injected = await _textInjector.InjectAsync(
                    result.CharactersToDelete,
                    result.ExpandedContent,
                    cancellationToken);

                if (injected && result.Snippet != null)
                {
                    await _engine.RecordExpansionUsageAsync(result.Snippet.Id, cancellationToken);
                }
            }
            finally
            {
                _engine.ResetBuffer();
                IsPaused = false;
            }
        }
    }

    public async Task<bool> CompleteInteractiveExpansionAsync(
        ExpansionResult pendingResult,
        IReadOnlyDictionary<string, string> userValues,
        IntPtr expectedWindowHandle,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pendingResult);
        ArgumentNullException.ThrowIfNull(userValues);

        // Validar que el foco no haya cambiado a otra ventana
        if (_focusTracker.HasFocusChanged(expectedWindowHandle))
        {
            _engine.ResetBuffer();
            return false;
        }

        // Validar que no se haya movido a un campo sensible
        if (_sensitiveDetector.IsSensitiveControlFocused())
        {
            _engine.ResetBuffer();
            return false;
        }

        if (pendingResult.Snippet == null || pendingResult.ExpandedContent == null)
        {
            _engine.ResetBuffer();
            return false;
        }

        var finalizedText = _engine.FinalizeInteractiveExpansion(
            pendingResult.Snippet,
            pendingResult.ExpandedContent,
            userValues);

        IsPaused = true;
        try
        {
            bool injected = await _textInjector.InjectAsync(
                pendingResult.CharactersToDelete,
                finalizedText,
                cancellationToken);

            if (injected && pendingResult.Snippet != null)
            {
                await _engine.RecordExpansionUsageAsync(pendingResult.Snippet.Id, cancellationToken);
            }

            return injected;
        }
        finally
        {
            _engine.ResetBuffer();
            IsPaused = false;
        }
    }

    private char? TranslateKeyToChar(RawKeyEvent keyEvent)
    {
        if (keyEvent.Character.HasValue)
        {
            return keyEvent.Character.Value;
        }

        switch (keyEvent.VirtualKeyCode)
        {
            case Win32Constants.VK_BACK:
                return '\b';
            case Win32Constants.VK_TAB:
                return '\t';
            case Win32Constants.VK_RETURN:
                return '\n';
            case Win32Constants.VK_SPACE:
                return ' ';
        }

        // Ignorar modificadores aislados
        if (keyEvent.VirtualKeyCode is Win32Constants.VK_SHIFT
            or Win32Constants.VK_CONTROL
            or Win32Constants.VK_MENU
            or Win32Constants.VK_CAPITAL)
        {
            return null;
        }

        try
        {
            var keyboardState = new byte[256];
            _win32.GetKeyboardState(keyboardState);

            // Sincronizar modificadores físicos globales en worker threads (Shift para ':', AltGr para '@', etc.)
            if ((_win32.GetAsyncKeyState(Win32Constants.VK_SHIFT) & 0x8000) != 0)
            {
                keyboardState[Win32Constants.VK_SHIFT] = 0x80;
            }
            if ((_win32.GetAsyncKeyState(Win32Constants.VK_CONTROL) & 0x8000) != 0)
            {
                keyboardState[Win32Constants.VK_CONTROL] = 0x80;
            }
            if ((_win32.GetAsyncKeyState(Win32Constants.VK_MENU) & 0x8000) != 0)
            {
                keyboardState[Win32Constants.VK_MENU] = 0x80;
            }
            if ((_win32.GetKeyState(Win32Constants.VK_CAPITAL) & 0x0001) != 0)
            {
                keyboardState[Win32Constants.VK_CAPITAL] = 0x01;
            }

            var sb = new StringBuilder(4);
            var hWnd = _focusTracker.GetActiveWindowHandle();
            uint threadId = _win32.GetWindowThreadProcessId(hWnd, out _);
            var hkl = _win32.GetKeyboardLayout(threadId);

            int res = _win32.ToUnicodeEx(
                (uint)keyEvent.VirtualKeyCode,
                (uint)keyEvent.ScanCode,
                keyboardState,
                sb,
                sb.Capacity,
                4, // wFlags = 4: no alterar dead keys
                hkl);

            if (res == 1)
            {
                return sb[0];
            }
            if (res > 1 && sb.Length > 0)
            {
                return sb[sb.Length - 1];
            }
        }
        catch
        {
            // Ignorar errores en resolución de caracteres nativos
        }

        return null;
    }

    private static bool IsNavigationOrResetKey(int vk)
    {
        return vk is Win32Constants.VK_ESCAPE
            or Win32Constants.VK_LEFT
            or Win32Constants.VK_RIGHT
            or Win32Constants.VK_UP
            or Win32Constants.VK_DOWN
            or Win32Constants.VK_HOME
            or Win32Constants.VK_END
            or Win32Constants.VK_PRIOR
            or Win32Constants.VK_NEXT
            or Win32Constants.VK_DELETE;
    }

    public void Dispose()
    {
        lock (_syncLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _cts?.Cancel();
            _cts?.Dispose();
            _hook.Dispose();
        }
    }
}
