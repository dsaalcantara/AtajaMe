using System.IO;
using AtajaMe.Application.Csv.DTOs;
using AtajaMe.Application.Csv.Services;
using AtajaMe.Application.DTOs;
using AtajaMe.Application.Expansion;
using AtajaMe.Application.Folders;
using AtajaMe.Application.Services;
using AtajaMe.Application.Snippets;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Enums;
using AtajaMe.Domain.Repositories;
using AtajaMe.Tests.Windows;
using AtajaMe.UI.ViewModels;

namespace AtajaMe.Tests.UI;

public sealed class MainViewModelTests
{
    private readonly FakeSnippetRepository _snippetRepo;
    private readonly FakeFolderRepository _folderRepo;
    private readonly FakeCsvService _csvService;
    private readonly FakeOrchestrator _orchestrator;
    private readonly MainViewModel _vm;

    public MainViewModelTests()
    {
        _snippetRepo = new FakeSnippetRepository();
        _folderRepo = new FakeFolderRepository();
        _csvService = new FakeCsvService();
        _orchestrator = new FakeOrchestrator();

        var getSnippets = new GetSnippetsUseCase(_snippetRepo);
        var createSnippet = new CreateSnippetUseCase(_snippetRepo, _folderRepo);
        var updateSnippet = new UpdateSnippetUseCase(_snippetRepo, _folderRepo);
        var deleteSnippet = new DeleteSnippetToTrashUseCase(_snippetRepo);
        var restoreSnippet = new RestoreSnippetUseCase(_snippetRepo);
        var purgeTrash = new PurgeTrashUseCase(_snippetRepo);
        var duplicateSnippet = new DuplicateSnippetUseCase(_snippetRepo);
        var toggleSnippet = new ToggleSnippetStateUseCase(_snippetRepo);

        var getFolders = new GetFoldersUseCase(_folderRepo);
        var createFolder = new CreateFolderUseCase(_folderRepo);
        var updateFolder = new UpdateFolderUseCase(_folderRepo);
        var deleteFolder = new DeleteFolderUseCase(_folderRepo);

        _vm = new MainViewModel(
            getSnippets,
            createSnippet,
            updateSnippet,
            deleteSnippet,
            restoreSnippet,
            purgeTrash,
            duplicateSnippet,
            toggleSnippet,
            getFolders,
            createFolder,
            updateFolder,
            deleteFolder,
            _csvService,
            _orchestrator);
    }

    [Fact]
    public async Task LoadDataAsync_PopulatesCountsAndLists()
    {
        // Arrange
        var s1 = Snippet.Create("s1", "Snippet 1", "Content 1");
        var s2 = Snippet.Create("s2", "Snippet 2", "Content 2");
        s2.ToggleFavorite();
        s2.RecordUsage();
        var s3 = Snippet.Create("s3", "Snippet 3", "Content 3");
        s3.MoveToTrash();

        await _snippetRepo.AddAsync(s1);
        await _snippetRepo.AddAsync(s2);
        await _snippetRepo.AddAsync(s3);

        // Act
        await _vm.LoadDataAsync();

        // Assert
        Assert.Equal(2, _vm.AllCount);
        Assert.Equal(1, _vm.FavoritesCount);
        Assert.Equal(1, _vm.RecentCount);
        Assert.Equal(1, _vm.TrashCount);
        Assert.Equal(2, _vm.DisplayedSnippets.Count);
    }

    [Fact]
    public async Task SearchQuery_FiltersByCodeNameOrContentInRealTime()
    {
        // Arrange
        var s1 = Snippet.Create("correo", "Email Trabajo", "contacto@empresa.com");
        var s2 = Snippet.Create("saludo", "Saludo General", "Hola a todos");
        await _snippetRepo.AddAsync(s1);
        await _snippetRepo.AddAsync(s2);
        await _vm.LoadDataAsync();

        // Act & Assert 1: búsqueda por código
        _vm.SearchQuery = "corr";
        Assert.Single(_vm.DisplayedSnippets);
        Assert.Equal("correo", _vm.DisplayedSnippets[0].Code);

        // Act & Assert 2: búsqueda por contenido
        _vm.SearchQuery = "empresa";
        Assert.Single(_vm.DisplayedSnippets);
        Assert.Equal("correo", _vm.DisplayedSnippets[0].Code);

        // Act & Assert 3: búsqueda vacía
        _vm.SearchQuery = string.Empty;
        Assert.Equal(2, _vm.DisplayedSnippets.Count);
    }

    [Fact]
    public async Task NavigationSections_FilterDisplayedSnippetsCorrectly()
    {
        // Arrange
        var s1 = Snippet.Create("fav", "Favorito", "Contenido");
        s1.ToggleFavorite();
        var s2 = Snippet.Create("norm", "Normal", "Contenido");
        var s3 = Snippet.Create("del", "Borrado", "Contenido");
        s3.MoveToTrash();

        await _snippetRepo.AddAsync(s1);
        await _snippetRepo.AddAsync(s2);
        await _snippetRepo.AddAsync(s3);
        await _vm.LoadDataAsync();

        // Favoritos
        _vm.SelectFavoritesCommand.Execute(null);
        Assert.Single(_vm.DisplayedSnippets);
        Assert.Equal("fav", _vm.DisplayedSnippets[0].Code);

        // Papelera
        _vm.SelectTrashCommand.Execute(null);
        Assert.Single(_vm.DisplayedSnippets);
        Assert.Equal("del", _vm.DisplayedSnippets[0].Code);

        // Todos
        _vm.SelectAllCommand.Execute(null);
        Assert.Equal(2, _vm.DisplayedSnippets.Count);
    }

    [Fact]
    public async Task ToggleFavoriteCommand_UpdatesFavoriteStatusAndCounts()
    {
        var s1 = Snippet.Create("t1", "Item", "Content");
        await _snippetRepo.AddAsync(s1);
        await _vm.LoadDataAsync();

        Assert.Equal(0, _vm.FavoritesCount);

        var itemVm = _vm.DisplayedSnippets[0];
        _vm.ToggleFavoriteCommand.Execute(itemVm);

        // Esperar tarea en cola
        await Task.Delay(50);

        Assert.True(itemVm.IsFavorite);
        Assert.Equal(1, _vm.FavoritesCount);
    }

    [Fact]
    public async Task DuplicateSnippetCommand_CreatesDuplicateWithSuffix()
    {
        var s1 = Snippet.Create("sig", "Firma", "Mi firma");
        await _snippetRepo.AddAsync(s1);
        await _vm.LoadDataAsync();

        var itemVm = _vm.DisplayedSnippets[0];
        _vm.DuplicateSnippetCommand.Execute(itemVm);

        await Task.Delay(50);

        Assert.Equal(2, _vm.AllCount);
        Assert.Contains(_vm.DisplayedSnippets, s => s.Code == "sig_copia");
    }

    [Fact]
    public void TogglePauseOrchestratorCommand_TogglesOrchestratorPausedState()
    {
        Assert.False(_vm.IsOrchestratorPaused);

        _vm.TogglePauseOrchestratorCommand.Execute(null);
        Assert.True(_vm.IsOrchestratorPaused);
        Assert.True(_orchestrator.IsPaused);

        _vm.TogglePauseOrchestratorCommand.Execute(null);
        Assert.False(_vm.IsOrchestratorPaused);
        Assert.False(_orchestrator.IsPaused);
    }

    #region Fakes

    private sealed class FakeFolderRepository : IFolderRepository
    {
        private readonly List<Folder> _folders = new();

        public Task<IReadOnlyList<Folder>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Folder>>(_folders);

        public Task<Folder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_folders.FirstOrDefault(f => f.Id == id));

        public Task AddAsync(Folder folder, CancellationToken cancellationToken = default)
        {
            _folders.Add(folder);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Folder folder, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _folders.RemoveAll(f => f.Id == id);
            return Task.CompletedTask;
        }

        public Task<int> GetDepthAsync(Guid folderId, CancellationToken cancellationToken = default)
            => Task.FromResult(1);
    }

    private sealed class FakeCsvService : ICsvImportExportService
    {
        public Task ExportAllAsync(TextWriter writer, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ExportFolderAsync(Guid folderId, TextWriter writer, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ExportSnippetAsync(Guid snippetId, TextWriter writer, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<CsvImportPreview> AnalyzeImportAsync(TextReader reader, CancellationToken cancellationToken = default)
            => Task.FromResult(new CsvImportPreview(0, 0, 0, 0, Array.Empty<CsvImportItemPreview>()));
        public Task<CsvImportResult> ExecuteImportAsync(TextReader reader, IReadOnlyDictionary<string, CsvConflictResolution>? resolutionOverrides = null, CsvConflictResolution defaultResolution = CsvConflictResolution.Skip, CancellationToken cancellationToken = default)
            => Task.FromResult(new CsvImportResult(0, 0, 0, 0, 0, Array.Empty<string>()));
    }

    private sealed class FakeOrchestrator : IKeyboardExpansionOrchestrator
    {
#pragma warning disable CS0067
        public event EventHandler<InteractiveInputRequestedEventArgs>? InteractiveInputRequested;
#pragma warning restore CS0067
        public bool IsRunning => true;
        public bool IsPaused { get; set; }
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> CompleteInteractiveExpansionAsync(ExpansionResult pendingResult, IReadOnlyDictionary<string, string> userValues, IntPtr expectedWindowHandle, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
        public void Dispose() { }
    }

    #endregion
}
