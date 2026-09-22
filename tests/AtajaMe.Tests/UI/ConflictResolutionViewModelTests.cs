using AtajaMe.Application.Sync.DTOs;
using AtajaMe.Application.Sync.Services;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Enums;
using AtajaMe.UI.ViewModels;

namespace AtajaMe.Tests.UI;

public sealed class ConflictResolutionViewModelTests
{
    [Fact]
    public async Task InitializeAsync_LoadsConflictsAndSelectsFirst()
    {
        var local = Snippet.Create("bd", "Buenos días", "Contenido local");
        var remote = Snippet.Create("bd", "Buenos días", "Contenido remoto");
        var conflict = SyncConflict.Create(local.Id, SyncConflictType.ConcurrentModification, local, remote);

        var fakeEngine = new FakeSyncEngine(new[] { conflict });
        var vm = new ConflictResolutionViewModel(fakeEngine);

        await vm.InitializeAsync();

        Assert.Single(vm.Conflicts);
        Assert.NotNull(vm.SelectedConflict);
        Assert.Equal("bd", vm.SelectedConflict.Code);
        Assert.Equal("Contenido local", vm.SelectedConflict.LocalContent);
        Assert.Equal("Contenido remoto", vm.SelectedConflict.RemoteContent);
    }

    [Fact]
    public async Task KeepLocalCommand_ResolvesConflict_AndClosesWhenEmpty()
    {
        var local = Snippet.Create("bd", "Buenos días", "Contenido local");
        var remote = Snippet.Create("bd", "Buenos días", "Contenido remoto");
        var conflict = SyncConflict.Create(local.Id, SyncConflictType.ConcurrentModification, local, remote);

        var fakeEngine = new FakeSyncEngine(new[] { conflict });
        var vm = new ConflictResolutionViewModel(fakeEngine);
        await vm.InitializeAsync();

        bool closed = false;
        vm.RequestClose += () => closed = true;

        vm.KeepLocalCommand.Execute(null);

        Assert.Empty(vm.Conflicts);
        Assert.True(closed);
        Assert.Equal(SyncResolutionChoice.KeepLocal, fakeEngine.LastChoice);
    }

    private sealed class FakeSyncEngine : ISyncEngine
    {
        private readonly List<SyncConflict> _conflicts;
        public SyncResolutionChoice? LastChoice { get; private set; }

        public FakeSyncEngine(IEnumerable<SyncConflict> conflicts)
        {
            _conflicts = conflicts.ToList();
        }

        public Task<SyncManifestDto> ExportManifestAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SyncResult> ProcessRemoteManifestAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SyncResult.Succeeded());
        }

        public Task ResolveConflictAsync(SyncConflict conflict, SyncResolutionChoice choice, CancellationToken cancellationToken = default)
        {
            LastChoice = choice;
            _conflicts.Remove(conflict);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SyncConflict>> GetPendingConflictsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<SyncConflict>>(_conflicts);
        }
    }
}
