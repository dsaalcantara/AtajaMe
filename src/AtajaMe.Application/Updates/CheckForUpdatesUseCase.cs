using AtajaMe.Domain.Updates;

namespace AtajaMe.Application.Updates;

public sealed class CheckForUpdatesUseCase
{
    private readonly IUpdateChecker _updateChecker;

    public CheckForUpdatesUseCase(IUpdateChecker updateChecker)
    {
        _updateChecker = updateChecker;
    }

    public async Task<UpdateInfo?> ExecuteAsync(Version currentVersion, CancellationToken cancellationToken = default)
    {
        return await _updateChecker.CheckForUpdatesAsync(currentVersion, cancellationToken);
    }
}
