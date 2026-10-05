using System.Collections.ObjectModel;
using System.Windows.Input;
using AtajaMe.Application.Csv.Services;
using AtajaMe.Application.DTOs;
using AtajaMe.Application.Exceptions;
using AtajaMe.Application.Folders;
using AtajaMe.Application.Services;
using AtajaMe.Application.Snippets;
using AtajaMe.UI.Common;
using AtajaMe.UI.Services;

namespace AtajaMe.UI.ViewModels;

public enum NavigationSection
{
    All,
    Favorites,
    Recent,
    Trash,
    Folder
}

/// <summary>
/// ViewModel principal que orquesta la navegación lateral, lista de atajos, filtros y edición (RF-01 a RF-19).
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    private readonly GetSnippetsUseCase _getSnippetsUseCase;
    private readonly CreateSnippetUseCase _createSnippetUseCase;
    private readonly UpdateSnippetUseCase _updateSnippetUseCase;
    private readonly DeleteSnippetToTrashUseCase _deleteSnippetUseCase;
    private readonly RestoreSnippetUseCase _restoreSnippetUseCase;
    private readonly PurgeTrashUseCase _purgeTrashUseCase;
    private readonly DuplicateSnippetUseCase _duplicateSnippetUseCase;
    private readonly ToggleSnippetStateUseCase _toggleSnippetUseCase;

    private readonly GetFoldersUseCase _getFoldersUseCase;
    private readonly CreateFolderUseCase _createFolderUseCase;
    private readonly UpdateFolderUseCase _updateFolderUseCase;
    private readonly DeleteFolderUseCase _deleteFolderUseCase;

    private readonly ICsvImportExportService _csvService;
    private readonly IKeyboardExpansionOrchestrator _orchestrator;
    private readonly IFolderDialogService _folderDialogService;

    private NavigationSection _currentSection = NavigationSection.All;
    private FolderTreeItemViewModel? _selectedFolder;
    private SnippetItemViewModel? _selectedSnippet;
    private string _searchQuery = string.Empty;
    private bool _isSortAlphabetical = true;
    private bool _isOrchestratorPaused;
    private string _statusMessage = string.Empty;

    private int _allCount;
    private int _favoritesCount;
    private int _recentCount;
    private int _trashCount;

    private List<SnippetDto> _allCachedSnippets = new();
    private List<FolderDto> _allCachedFolders = new();

    public NavigationSection CurrentSection
    {
        get => _currentSection;
        set
        {
            if (SetProperty(ref _currentSection, value))
            {
                OnPropertyChanged(nameof(IsAllSelected));
                OnPropertyChanged(nameof(IsFavoritesSelected));
                OnPropertyChanged(nameof(IsRecentSelected));
                OnPropertyChanged(nameof(IsTrashSelected));
                OnPropertyChanged(nameof(IsFolderSelected));
                OnPropertyChanged(nameof(CurrentSectionTitle));
                ApplyFilter();
            }
        }
    }

    public bool IsAllSelected => CurrentSection == NavigationSection.All;
    public bool IsFavoritesSelected => CurrentSection == NavigationSection.Favorites;
    public bool IsRecentSelected => CurrentSection == NavigationSection.Recent;
    public bool IsTrashSelected => CurrentSection == NavigationSection.Trash;
    public bool IsFolderSelected => CurrentSection == NavigationSection.Folder;

    public string CurrentSectionTitle => CurrentSection switch
    {
        NavigationSection.All => "Todos los Atajos",
        NavigationSection.Favorites => "Favoritos",
        NavigationSection.Recent => "Recientes",
        NavigationSection.Trash => "Papelera de Reciclaje",
        NavigationSection.Folder => SelectedFolder != null ? SelectedFolder.Name : "Carpeta",
        _ => "Atajos"
    };

    public FolderTreeItemViewModel? SelectedFolder
    {
        get => _selectedFolder;
        set
        {
            if (SetProperty(ref _selectedFolder, value))
            {
                if (value != null)
                {
                    _currentSection = NavigationSection.Folder;
                    OnPropertyChanged(nameof(CurrentSection));
                    OnPropertyChanged(nameof(IsAllSelected));
                    OnPropertyChanged(nameof(IsFavoritesSelected));
                    OnPropertyChanged(nameof(IsRecentSelected));
                    OnPropertyChanged(nameof(IsTrashSelected));
                    OnPropertyChanged(nameof(IsFolderSelected));
                    OnPropertyChanged(nameof(CurrentSectionTitle));
                    ApplyFilter();
                }
            }
        }
    }

    public SnippetItemViewModel? SelectedSnippet
    {
        get => _selectedSnippet;
        set
        {
            if (SetProperty(ref _selectedSnippet, value))
            {
                if (value != null)
                {
                    var dto = _allCachedSnippets.FirstOrDefault(s => s.Id == value.Id);
                    if (dto != null)
                    {
                        Editor.LoadSnippet(dto);
                    }
                }
                else if (!Editor.IsNew)
                {
                    Editor.Clear();
                }
            }
        }
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                ApplyFilter();
            }
        }
    }

    public bool IsSortAlphabetical
    {
        get => _isSortAlphabetical;
        set
        {
            if (SetProperty(ref _isSortAlphabetical, value))
            {
                ApplyFilter();
            }
        }
    }

    public bool IsOrchestratorPaused
    {
        get => _isOrchestratorPaused;
        set => SetProperty(ref _isOrchestratorPaused, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public int AllCount { get => _allCount; private set => SetProperty(ref _allCount, value); }
    public int FavoritesCount { get => _favoritesCount; private set => SetProperty(ref _favoritesCount, value); }
    public int RecentCount { get => _recentCount; private set => SetProperty(ref _recentCount, value); }
    public int TrashCount { get => _trashCount; private set => SetProperty(ref _trashCount, value); }

    public ObservableCollection<FolderTreeItemViewModel> FolderTree { get; } = new();
    public ObservableCollection<SnippetItemViewModel> DisplayedSnippets { get; } = new();

    public SnippetEditorViewModel Editor { get; } = new();

    #region Comandos

    public ICommand SelectAllCommand { get; }
    public ICommand SelectFavoritesCommand { get; }
    public ICommand SelectRecentCommand { get; }
    public ICommand SelectTrashCommand { get; }

    public ICommand NewSnippetCommand { get; }
    public ICommand DuplicateSnippetCommand { get; }
    public ICommand DeleteSnippetCommand { get; }
    public ICommand RestoreSnippetCommand { get; }
    public ICommand PurgeTrashCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand ToggleActiveCommand { get; }

    public ICommand NewFolderCommand { get; }
    public ICommand NewSubFolderCommand { get; }
    public ICommand RenameFolderCommand { get; }
    public ICommand DeleteFolderCommand { get; }

    public ICommand RefreshCommand { get; }
    public ICommand TogglePauseOrchestratorCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand ImportCsvCommand { get; }

    #endregion

    public event Action? RequestExportCsv;
    public event Action? RequestImportCsv;

    public MainViewModel(
        GetSnippetsUseCase getSnippetsUseCase,
        CreateSnippetUseCase createSnippetUseCase,
        UpdateSnippetUseCase updateSnippetUseCase,
        DeleteSnippetToTrashUseCase deleteSnippetUseCase,
        RestoreSnippetUseCase restoreSnippetUseCase,
        PurgeTrashUseCase purgeTrashUseCase,
        DuplicateSnippetUseCase duplicateSnippetUseCase,
        ToggleSnippetStateUseCase toggleSnippetUseCase,
        GetFoldersUseCase getFoldersUseCase,
        CreateFolderUseCase createFolderUseCase,
        UpdateFolderUseCase updateFolderUseCase,
        DeleteFolderUseCase deleteFolderUseCase,
        ICsvImportExportService csvService,
        IKeyboardExpansionOrchestrator orchestrator,
        IFolderDialogService? folderDialogService = null)
    {
        _getSnippetsUseCase = getSnippetsUseCase;
        _createSnippetUseCase = createSnippetUseCase;
        _updateSnippetUseCase = updateSnippetUseCase;
        _deleteSnippetUseCase = deleteSnippetUseCase;
        _restoreSnippetUseCase = restoreSnippetUseCase;
        _purgeTrashUseCase = purgeTrashUseCase;
        _duplicateSnippetUseCase = duplicateSnippetUseCase;
        _toggleSnippetUseCase = toggleSnippetUseCase;

        _getFoldersUseCase = getFoldersUseCase;
        _createFolderUseCase = createFolderUseCase;
        _updateFolderUseCase = updateFolderUseCase;
        _deleteFolderUseCase = deleteFolderUseCase;

        _csvService = csvService;
        _orchestrator = orchestrator;
        _isOrchestratorPaused = orchestrator.IsPaused;
        _folderDialogService = folderDialogService ?? new FolderDialogService();

        SelectAllCommand = new RelayCommand(() => CurrentSection = NavigationSection.All);
        SelectFavoritesCommand = new RelayCommand(() => CurrentSection = NavigationSection.Favorites);
        SelectRecentCommand = new RelayCommand(() => CurrentSection = NavigationSection.Recent);
        SelectTrashCommand = new RelayCommand(() => CurrentSection = NavigationSection.Trash);

        NewSnippetCommand = new RelayCommand(ExecuteNewSnippet);
        DuplicateSnippetCommand = new AsyncRelayCommand(ExecuteDuplicateSnippetAsync);
        DeleteSnippetCommand = new AsyncRelayCommand(ExecuteDeleteSnippetAsync);
        RestoreSnippetCommand = new AsyncRelayCommand(ExecuteRestoreSnippetAsync);
        PurgeTrashCommand = new AsyncRelayCommand(ExecutePurgeTrashAsync);
        ToggleFavoriteCommand = new AsyncRelayCommand(ExecuteToggleFavoriteAsync);
        ToggleActiveCommand = new AsyncRelayCommand(ExecuteToggleActiveAsync);

        NewFolderCommand = new AsyncRelayCommand(ExecuteNewFolderAsync);
        NewSubFolderCommand = new AsyncRelayCommand(ExecuteNewSubFolderAsync);
        RenameFolderCommand = new AsyncRelayCommand(ExecuteRenameFolderAsync);
        DeleteFolderCommand = new AsyncRelayCommand(ExecuteDeleteFolderAsync);

        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        TogglePauseOrchestratorCommand = new RelayCommand(ExecuteTogglePause);
        ExportCsvCommand = new RelayCommand(() => RequestExportCsv?.Invoke());
        ImportCsvCommand = new RelayCommand(() => RequestImportCsv?.Invoke());

        Editor.OnSaveRequested += HandleEditorSaveAsync;
        Editor.OnCancelRequested += HandleEditorCancel;
    }

    public async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var folders = await _getFoldersUseCase.GetAllAsync();
            _allCachedFolders = folders.ToList();

            var library = await _getSnippetsUseCase.GetAllAsync();
            var trash = await _getSnippetsUseCase.GetTrashAsync();
            _allCachedSnippets = library.Concat(trash).ToList();

            UpdateFolderTree();
            Editor.UpdateFolders(_allCachedFolders);
            UpdateCounts();
            ApplyFilter();

            StatusMessage = $"Biblioteca cargada: {_allCachedSnippets.Count(s => !s.IsInTrash)} atajos";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al cargar biblioteca: {ex.Message}";
        }
    }

    private void UpdateCounts()
    {
        AllCount = _allCachedSnippets.Count(s => !s.IsInTrash);
        FavoritesCount = _allCachedSnippets.Count(s => s.IsFavorite && !s.IsInTrash);
        RecentCount = _allCachedSnippets.Count(s => s.LastUsedAt.HasValue && !s.IsInTrash);
        TrashCount = _allCachedSnippets.Count(s => s.IsInTrash);
    }

    private void UpdateFolderTree()
    {
        FolderTree.Clear();
        var roots = _allCachedFolders.Where(f => f.ParentId == null).OrderBy(f => f.SortOrder).ThenBy(f => f.Name);
        foreach (var root in roots)
        {
            var node = CreateFolderNodeRecursive(root, 1);
            FolderTree.Add(node);
        }
    }

    private FolderTreeItemViewModel CreateFolderNodeRecursive(FolderDto current, int level)
    {
        var node = new FolderTreeItemViewModel(current, level)
        {
            SnippetCount = _allCachedSnippets.Count(s => s.FolderId == current.Id && !s.IsInTrash)
        };

        var children = _allCachedFolders.Where(f => f.ParentId == current.Id).OrderBy(f => f.SortOrder).ThenBy(f => f.Name);
        foreach (var child in children)
        {
            node.Children.Add(CreateFolderNodeRecursive(child, level + 1));
        }

        return node;
    }

    private void ApplyFilter()
    {
        IEnumerable<SnippetDto> query = _allCachedSnippets;

        // Filtro por sección o carpeta
        switch (CurrentSection)
        {
            case NavigationSection.All:
                query = query.Where(s => !s.IsInTrash);
                break;
            case NavigationSection.Favorites:
                query = query.Where(s => s.IsFavorite && !s.IsInTrash);
                break;
            case NavigationSection.Recent:
                query = query.Where(s => s.LastUsedAt.HasValue && !s.IsInTrash).OrderByDescending(s => s.LastUsedAt);
                break;
            case NavigationSection.Trash:
                query = query.Where(s => s.IsInTrash);
                break;
            case NavigationSection.Folder:
                if (SelectedFolder != null)
                {
                    query = query.Where(s => s.FolderId == SelectedFolder.Id && !s.IsInTrash);
                }
                else
                {
                    query = query.Where(s => !s.IsInTrash);
                }
                break;
        }

        // Filtro por búsqueda de texto
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.Trim();
            query = query.Where(s =>
                s.Code.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                s.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                s.Content.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        // Ordenación
        if (CurrentSection != NavigationSection.Recent)
        {
            query = IsSortAlphabetical
                ? query.OrderBy(s => s.Name).ThenBy(s => s.Code)
                : query.OrderBy(s => s.SortOrder).ThenBy(s => s.Name);
        }

        var folderMap = _allCachedFolders.ToDictionary(f => f.Id, f => f.Name);

        DisplayedSnippets.Clear();
        foreach (var item in query)
        {
            var folderName = item.FolderId.HasValue && folderMap.TryGetValue(item.FolderId.Value, out var fn) ? fn : null;
            DisplayedSnippets.Add(new SnippetItemViewModel(item, folderName));
        }

        // Mantener selección sincronizada si existe
        if (SelectedSnippet != null)
        {
            var match = DisplayedSnippets.FirstOrDefault(s => s.Id == SelectedSnippet.Id);
            if (match != null)
            {
                _selectedSnippet = match;
                OnPropertyChanged(nameof(SelectedSnippet));
            }
        }
    }

    private void ExecuteNewSnippet()
    {
        SelectedSnippet = null;
        Guid? defaultFolderId = CurrentSection == NavigationSection.Folder ? SelectedFolder?.Id : null;
        Editor.PrepareNewSnippet(defaultFolderId);
    }

    private async Task ExecuteDuplicateSnippetAsync(object? parameter)
    {
        var targetId = (parameter as SnippetItemViewModel)?.Id ?? SelectedSnippet?.Id;
        if (!targetId.HasValue)
        {
            return;
        }

        var original = _allCachedSnippets.FirstOrDefault(s => s.Id == targetId.Value);
        if (original == null)
        {
            return;
        }

        try
        {
            var newCode = $"{original.Code}_copia";
            int counter = 1;
            while (_allCachedSnippets.Any(s => s.MatchesCode(newCode)))
            {
                counter++;
                newCode = $"{original.Code}_copia{counter}";
            }

            var duplicate = await _duplicateSnippetUseCase.ExecuteAsync(original.Id, newCode);
            await LoadDataAsync();

            var newVm = DisplayedSnippets.FirstOrDefault(s => s.Id == duplicate.Id);
            if (newVm != null)
            {
                SelectedSnippet = newVm;
            }

            StatusMessage = $"Atajo duplicado como '{newCode}'";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al duplicar atajo: {ex.Message}";
        }
    }

    private async Task ExecuteDeleteSnippetAsync(object? parameter)
    {
        var targetId = (parameter as SnippetItemViewModel)?.Id ?? SelectedSnippet?.Id;
        if (!targetId.HasValue)
        {
            return;
        }

        try
        {
            await _deleteSnippetUseCase.ExecuteAsync(targetId.Value);
            Editor.Clear();
            SelectedSnippet = null;
            await LoadDataAsync();
            StatusMessage = "Atajo trasladado a la papelera";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al eliminar: {ex.Message}";
        }
    }

    private async Task ExecuteRestoreSnippetAsync(object? parameter)
    {
        var targetId = (parameter as SnippetItemViewModel)?.Id ??
                       (parameter as SnippetEditorViewModel)?.Id ??
                       SelectedSnippet?.Id ??
                       Editor.Id;
        if (!targetId.HasValue)
        {
            return;
        }

        try
        {
            await _restoreSnippetUseCase.ExecuteAsync(targetId.Value);
            await LoadDataAsync();
            var restored = DisplayedSnippets.FirstOrDefault(s => s.Id == targetId.Value);
            if (restored != null)
            {
                SelectedSnippet = restored;
            }
            else
            {
                var dto = _allCachedSnippets.FirstOrDefault(s => s.Id == targetId.Value);
                if (dto != null)
                {
                    Editor.LoadSnippet(dto);
                }
            }
            StatusMessage = "Atajo restaurado con éxito";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al restaurar: {ex.Message}";
        }
    }

    private async Task ExecutePurgeTrashAsync()
    {
        try
        {
            await _purgeTrashUseCase.PurgeAllAsync();
            Editor.Clear();
            SelectedSnippet = null;
            await LoadDataAsync();
            StatusMessage = "Papelera vaciada por completo.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al vaciar papelera: {ex.Message}";
        }
    }

    private async Task ExecuteToggleFavoriteAsync(object? parameter)
    {
        var vm = parameter as SnippetItemViewModel ?? SelectedSnippet;
        if (vm == null)
        {
            return;
        }

        try
        {
            await _toggleSnippetUseCase.ToggleFavoriteAsync(vm.Id);
            var snippet = _allCachedSnippets.FirstOrDefault(s => s.Id == vm.Id);
            if (snippet != null)
            {
                var updated = snippet with { IsFavorite = !snippet.IsFavorite };
                _allCachedSnippets[_allCachedSnippets.IndexOf(snippet)] = updated;
                vm.IsFavorite = updated.IsFavorite;
                if (Editor.Id == vm.Id)
                {
                    Editor.IsFavorite = vm.IsFavorite;
                }
            }
            UpdateCounts();
            if (CurrentSection == NavigationSection.Favorites)
            {
                ApplyFilter();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al actualizar favorito: {ex.Message}";
        }
    }

    private async Task ExecuteToggleActiveAsync(object? parameter)
    {
        var vm = parameter as SnippetItemViewModel ?? SelectedSnippet;
        if (vm == null)
        {
            return;
        }

        try
        {
            await _toggleSnippetUseCase.ToggleActiveAsync(vm.Id);
            var snippet = _allCachedSnippets.FirstOrDefault(s => s.Id == vm.Id);
            if (snippet != null)
            {
                var updated = snippet with { IsActive = !snippet.IsActive };
                _allCachedSnippets[_allCachedSnippets.IndexOf(snippet)] = updated;
                vm.IsActive = updated.IsActive;
                if (Editor.Id == vm.Id)
                {
                    Editor.IsActive = vm.IsActive;
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al cambiar estado activo: {ex.Message}";
        }
    }

    private async Task ExecuteNewFolderAsync()
    {
        try
        {
            var folderName = await _folderDialogService.PromptFolderNameAsync(
                "Nueva Carpeta",
                "Introduce el nombre de la nueva carpeta:",
                "Nueva Carpeta");

            if (string.IsNullOrWhiteSpace(folderName))
            {
                return;
            }

            var folder = await _createFolderUseCase.ExecuteAsync(new CreateFolderCommand(folderName.Trim()));
            await LoadDataAsync();

            var createdNode = FindFolderNodeRecursive(FolderTree, folder.Id);
            if (createdNode != null)
            {
                SelectedFolder = createdNode;
            }

            StatusMessage = $"Carpeta '{folder.Name}' creada";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al crear carpeta: {ex.Message}";
        }
    }

    private async Task ExecuteNewSubFolderAsync(object? parameter)
    {
        var parentNode = parameter as FolderTreeItemViewModel ?? SelectedFolder;
        if (parentNode == null)
        {
            return;
        }

        if (parentNode.Level >= 3)
        {
            StatusMessage = "No se pueden crear más de 3 niveles de carpetas.";
            return;
        }

        try
        {
            var subFolderName = await _folderDialogService.PromptFolderNameAsync(
                "Nueva Subcarpeta",
                $"Introduce el nombre para la subcarpeta dentro de '{parentNode.Name}':",
                "Subcarpeta");

            if (string.IsNullOrWhiteSpace(subFolderName))
            {
                return;
            }

            var folder = await _createFolderUseCase.ExecuteAsync(new CreateFolderCommand(subFolderName.Trim(), parentNode.Id));
            await LoadDataAsync();

            var createdNode = FindFolderNodeRecursive(FolderTree, folder.Id);
            if (createdNode != null)
            {
                SelectedFolder = createdNode;
            }

            StatusMessage = $"Subcarpeta '{folder.Name}' creada";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al crear subcarpeta: {ex.Message}";
        }
    }

    private async Task ExecuteRenameFolderAsync(object? parameter)
    {
        var node = parameter as FolderTreeItemViewModel ?? SelectedFolder;
        if (node == null)
        {
            return;
        }

        try
        {
            var newName = await _folderDialogService.PromptFolderNameAsync(
                "Renombrar Carpeta",
                "Introduce el nuevo nombre para la carpeta:",
                node.Name);

            if (string.IsNullOrWhiteSpace(newName) || newName.Trim() == node.Name)
            {
                return;
            }

            await _updateFolderUseCase.ExecuteAsync(new UpdateFolderCommand(node.Id, newName.Trim(), node.ParentId, node.SortOrder));
            await LoadDataAsync();

            var updatedNode = FindFolderNodeRecursive(FolderTree, node.Id);
            if (updatedNode != null)
            {
                SelectedFolder = updatedNode;
            }

            StatusMessage = $"Carpeta renombrada a '{newName.Trim()}'";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al renombrar: {ex.Message}";
        }
    }

    private async Task ExecuteDeleteFolderAsync(object? parameter)
    {
        var node = parameter as FolderTreeItemViewModel ?? SelectedFolder;
        if (node == null)
        {
            return;
        }

        try
        {
            bool confirmed = await _folderDialogService.ConfirmDeleteFolderAsync(node.Name, node.SnippetCount);
            if (!confirmed)
            {
                return;
            }

            await _deleteFolderUseCase.ExecuteAsync(node.Id);
            if (SelectedFolder?.Id == node.Id)
            {
                SelectedFolder = null;
                CurrentSection = NavigationSection.All;
            }
            await LoadDataAsync();
            StatusMessage = $"Carpeta '{node.Name}' eliminada";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al eliminar carpeta: {ex.Message}";
        }
    }

    private static FolderTreeItemViewModel? FindFolderNodeRecursive(IEnumerable<FolderTreeItemViewModel> nodes, Guid id)
    {
        foreach (var node in nodes)
        {
            if (node.Id == id)
            {
                return node;
            }

            var found = FindFolderNodeRecursive(node.Children, id);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void ExecuteTogglePause()
    {
        _orchestrator.IsPaused = !_orchestrator.IsPaused;
        IsOrchestratorPaused = _orchestrator.IsPaused;
        StatusMessage = IsOrchestratorPaused ? "Expansión de atajos pausada" : "Expansión de atajos activa";
    }

    private async Task HandleEditorSaveAsync(SnippetEditorViewModel editor)
    {
        try
        {
            if (editor.IsNew)
            {
                var command = new CreateSnippetCommand(
                    editor.Code,
                    editor.Name,
                    editor.Content,
                    editor.SelectedFolderId,
                    editor.CaseSensitive);

                var created = await _createSnippetUseCase.ExecuteAsync(command);

                if (!editor.IsActive)
                {
                    await _toggleSnippetUseCase.ToggleActiveAsync(created.Id);
                }
                if (editor.IsFavorite)
                {
                    await _toggleSnippetUseCase.ToggleFavoriteAsync(created.Id);
                }

                await LoadDataAsync();
                var createdVm = DisplayedSnippets.FirstOrDefault(s => s.Id == created.Id);
                if (createdVm != null)
                {
                    SelectedSnippet = createdVm;
                }
                StatusMessage = $"Atajo '{created.Code}' creado con éxito";
            }
            else if (editor.Id.HasValue)
            {
                var command = new UpdateSnippetCommand(
                    editor.Id.Value,
                    editor.Code,
                    editor.Name,
                    editor.Content,
                    editor.SelectedFolderId,
                    editor.CaseSensitive);

                var updated = await _updateSnippetUseCase.ExecuteAsync(command);

                // Asegurar sincronización de flags activo / favorito si variaron
                var currentDto = _allCachedSnippets.FirstOrDefault(s => s.Id == editor.Id.Value);
                if (currentDto != null)
                {
                    if (currentDto.IsActive != editor.IsActive)
                    {
                        await _toggleSnippetUseCase.ToggleActiveAsync(editor.Id.Value);
                    }
                    if (currentDto.IsFavorite != editor.IsFavorite)
                    {
                        await _toggleSnippetUseCase.ToggleFavoriteAsync(editor.Id.Value);
                    }
                }

                await LoadDataAsync();
                var updatedVm = DisplayedSnippets.FirstOrDefault(s => s.Id == updated.Id);
                if (updatedVm != null)
                {
                    SelectedSnippet = updatedVm;
                }
                StatusMessage = $"Atajo '{updated.Code}' guardado con éxito";
            }
        }
        catch (SnippetInTrashException)
        {
            editor.ErrorMessage = $"El código '{editor.Code}' ya existe en la Papelera de reciclaje. Puedes recuperarlo desde la sección 'Papelera'.";
            StatusMessage = editor.ErrorMessage;
        }
        catch (DuplicateSnippetCodeException)
        {
            editor.ErrorMessage = $"El código '{editor.Code}' ya está siendo utilizado por otro atajo.";
            StatusMessage = editor.ErrorMessage;
        }
        catch (Exception ex)
        {
            editor.ErrorMessage = ex.Message;
            StatusMessage = $"Error al guardar: {ex.Message}";
        }
    }

    private void HandleEditorCancel()
    {
        if (SelectedSnippet != null)
        {
            var dto = _allCachedSnippets.FirstOrDefault(s => s.Id == SelectedSnippet.Id);
            if (dto != null)
            {
                Editor.LoadSnippet(dto);
                return;
            }
        }
        Editor.Clear();
    }
}

internal static class SnippetDtoExtensions
{
    public static bool MatchesCode(this SnippetDto dto, string code)
    {
        var comparison = dto.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return string.Equals(dto.Code, code, comparison);
    }
}
