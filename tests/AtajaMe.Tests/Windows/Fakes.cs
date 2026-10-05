using System.Threading.Channels;
using AtajaMe.Application.Services;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Repositories;

namespace AtajaMe.Tests.Windows;

public sealed class FakeKeyboardHook : IKeyboardHook
{
    private readonly Channel<RawKeyEvent> _channel = Channel.CreateUnbounded<RawKeyEvent>();

    public ChannelReader<RawKeyEvent> KeyEvents => _channel.Reader;
    public bool IsRunning { get; private set; }

    public void Start() => IsRunning = true;
    public void Stop() => IsRunning = false;

    public void EmitKey(char c)
    {
        _channel.Writer.TryWrite(new RawKeyEvent(0, 0, 0, 0, character: c, isKeyDown: true));
    }

    public void EmitVk(int vkCode, char? character = null)
    {
        _channel.Writer.TryWrite(new RawKeyEvent(vkCode, 0, 0, 0, character: character, isKeyDown: true));
    }

    public void Dispose()
    {
        _channel.Writer.TryComplete();
        IsRunning = false;
    }
}

public sealed class FakeTextInjector : ITextInjector
{
    public List<(int Backspaces, string Text)> Injections { get; } = new();
    public bool ReturnValue { get; set; } = true;

    public Task<bool> InjectAsync(int backspacesToDelete, string textToInject, CancellationToken cancellationToken = default)
    {
        Injections.Add((backspacesToDelete, textToInject));
        return Task.FromResult(ReturnValue);
    }
}

public sealed class FakeFocusTracker : IFocusTracker
{
    public IntPtr ActiveWindow { get; set; } = new IntPtr(100);
    public string? ActiveProcessName { get; set; } = "notepad";
    public bool FocusChangedReturnValue { get; set; } = false;
    public bool IsCurrentAppActiveValue { get; set; } = false;

    public IntPtr GetActiveWindowHandle() => ActiveWindow;

    public string? GetActiveProcessName() => ActiveProcessName;

    public bool HasFocusChanged(IntPtr originalWindowHandle)
    {
        if (FocusChangedReturnValue) return true;
        return originalWindowHandle != ActiveWindow;
    }

    public bool IsCurrentAppActive() => IsCurrentAppActiveValue;
}

public sealed class FakeSensitiveDetector : ISensitiveControlDetector
{
    public bool IsSensitive { get; set; } = false;

    public bool IsSensitiveControlFocused() => IsSensitive;
}

public sealed class FakeExcludedAppRepository : IExcludedApplicationRepository
{
    private readonly List<ExcludedApplication> _apps = new();

    public Task<IReadOnlyList<ExcludedApplication>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ExcludedApplication>>(_apps);

    public Task<bool> IsExcludedAsync(string processName, CancellationToken cancellationToken = default)
    {
        bool excluded = _apps.Any(a => string.Equals(a.ProcessName, processName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(excluded);
    }

    public Task AddAsync(ExcludedApplication application, CancellationToken cancellationToken = default)
    {
        _apps.Add(application);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _apps.RemoveAll(a => a.Id == id);
        return Task.CompletedTask;
    }
}

public sealed class FakeSnippetRepository : ISnippetRepository
{
    private readonly List<Snippet> _snippets = new();

    public Task AddAsync(Snippet snippet, CancellationToken cancellationToken = default)
    {
        _snippets.Add(snippet);
        return Task.CompletedTask;
    }

    public Task<Snippet?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var match = _snippets.FirstOrDefault(s =>
            !s.IsInTrash && string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(match);
    }

    public Task<Snippet?> GetAnyByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var match = _snippets.FirstOrDefault(s =>
            string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(match);
    }

    public Task<Snippet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var match = _snippets.FirstOrDefault(s => s.Id == id);
        return Task.FromResult(match);
    }

    public Task UpdateAsync(Snippet snippet, CancellationToken cancellationToken = default)
    {
        var idx = _snippets.FindIndex(s => s.Id == snippet.Id);
        if (idx >= 0)
        {
            _snippets[idx] = snippet;
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Snippet>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Snippet>>(_snippets.Where(s => !s.IsInTrash).ToList());

    public Task<IReadOnlyList<Snippet>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Snippet>>(_snippets.Where(s => s.IsActive && !s.IsInTrash).ToList());

    public Task<IReadOnlyList<Snippet>> GetByFolderIdAsync(Guid? folderId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Snippet>>(_snippets.Where(s => s.FolderId == folderId).ToList());

    public Task<IReadOnlyList<Snippet>> GetFavoritesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Snippet>>(_snippets.Where(s => s.IsFavorite && !s.IsInTrash).ToList());

    public Task<IReadOnlyList<Snippet>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Snippet>>(_snippets.OrderByDescending(s => s.LastUsedAt).Take(limit).ToList());

    public Task<IReadOnlyList<Snippet>> GetTrashAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Snippet>>(_snippets.Where(s => s.IsInTrash).ToList());

    public Task<IReadOnlyList<Snippet>> SearchAsync(string query, Guid? folderId = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Snippet>>(_snippets);

    public Task DeletePermanentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _snippets.RemoveAll(s => s.Id == id);
        return Task.CompletedTask;
    }

    public Task PurgeTrashOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        _snippets.RemoveAll(s => s.IsInTrash && s.DeletedAt.HasValue && s.DeletedAt.Value < cutoffDate);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsCodeAsync(string code, Guid? excludeSnippetId = null, CancellationToken cancellationToken = default)
    {
        var exists = _snippets.Any(s =>
            string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase) &&
            (!excludeSnippetId.HasValue || s.Id != excludeSnippetId.Value));
        return Task.FromResult(exists);
    }
}
