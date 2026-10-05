using AtajaMe.Application.Csv.DTOs;
using AtajaMe.Application.Csv.Parser;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Enums;
using AtajaMe.Domain.Repositories;

namespace AtajaMe.Application.Csv.Services;

/// <summary>
/// Implementación de los servicios de exportación e importación CSV para AtajaMe (RF-25, RF-26, RF-27).
/// </summary>
public sealed class CsvImportExportService : ICsvImportExportService
{
    private readonly ISnippetRepository _snippetRepository;
    private readonly IFolderRepository _folderRepository;

    public CsvImportExportService(ISnippetRepository snippetRepository, IFolderRepository folderRepository)
    {
        _snippetRepository = snippetRepository ?? throw new ArgumentNullException(nameof(snippetRepository));
        _folderRepository = folderRepository ?? throw new ArgumentNullException(nameof(folderRepository));
    }

    public async Task ExportAllAsync(TextWriter writer, CancellationToken cancellationToken = default)
    {
        var snippets = await _snippetRepository.GetAllAsync(cancellationToken);
        var folderPathMap = await BuildFolderPathMapAsync(cancellationToken);

        var rows = snippets.Select(s => MapToRow(s, folderPathMap));
        CsvWriterHelper.Write(writer, rows);
    }

    public async Task ExportFolderAsync(Guid folderId, TextWriter writer, CancellationToken cancellationToken = default)
    {
        var allFolders = await _folderRepository.GetAllAsync(cancellationToken);
        var folderMap = allFolders.ToDictionary(f => f.Id);

        if (!folderMap.ContainsKey(folderId))
        {
            throw new InvalidOperationException($"La carpeta con ID {folderId} no existe.");
        }

        // Recolectar la carpeta y todas sus descendientes
        var targetFolderIds = new HashSet<Guid> { folderId };
        bool addedAny;
        do
        {
            addedAny = false;
            foreach (var folder in allFolders)
            {
                if (folder.ParentId.HasValue && targetFolderIds.Contains(folder.ParentId.Value) && !targetFolderIds.Contains(folder.Id))
                {
                    targetFolderIds.Add(folder.Id);
                    addedAny = true;
                }
            }
        } while (addedAny);

        var folderPathMap = BuildFolderPathMap(allFolders);

        var matchingSnippets = new List<Snippet>();
        foreach (var id in targetFolderIds)
        {
            var folderSnippets = await _snippetRepository.GetByFolderIdAsync(id, cancellationToken);
            matchingSnippets.AddRange(folderSnippets.Where(s => !s.IsInTrash));
        }

        var rows = matchingSnippets.Select(s => MapToRow(s, folderPathMap));
        CsvWriterHelper.Write(writer, rows);
    }

    public async Task ExportSnippetAsync(Guid snippetId, TextWriter writer, CancellationToken cancellationToken = default)
    {
        var snippet = await _snippetRepository.GetByIdAsync(snippetId, cancellationToken);
        if (snippet == null)
        {
            throw new InvalidOperationException($"El atajo con ID {snippetId} no existe.");
        }

        var folderPathMap = await BuildFolderPathMapAsync(cancellationToken);
        var row = MapToRow(snippet, folderPathMap);
        CsvWriterHelper.Write(writer, new[] { row });
    }

    public async Task<CsvImportPreview> AnalyzeImportAsync(TextReader reader, CancellationToken cancellationToken = default)
    {
        var rows = CsvReaderHelper.Read(reader);
        var items = new List<CsvImportItemPreview>(rows.Count);
        int validCount = 0;
        int conflictCount = 0;
        int invalidCount = 0;

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            string? validationError = ValidateRow(row);

            if (validationError != null)
            {
                invalidCount++;
                items.Add(new CsvImportItemPreview(i + 1, row, false, null, validationError));
                continue;
            }

            validCount++;
            var existingSnippet = await _snippetRepository.GetByCodeAsync(row.Code.Trim(), cancellationToken);
            bool hasConflict = existingSnippet != null;
            if (hasConflict)
            {
                conflictCount++;
            }

            items.Add(new CsvImportItemPreview(i + 1, row, hasConflict, existingSnippet, null));
        }

        return new CsvImportPreview(rows.Count, validCount, conflictCount, invalidCount, items);
    }

    public async Task<CsvImportResult> ExecuteImportAsync(
        TextReader reader,
        IReadOnlyDictionary<string, CsvConflictResolution>? resolutionOverrides = null,
        CsvConflictResolution defaultResolution = CsvConflictResolution.Skip,
        CancellationToken cancellationToken = default)
    {
        var rows = CsvReaderHelper.Read(reader);
        var errors = new List<string>();

        int imported = 0;
        int overwritten = 0;
        int duplicated = 0;
        int skipped = 0;

        var allFolders = (await _folderRepository.GetAllAsync(cancellationToken)).ToList();

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            string? error = ValidateRow(row);
            if (error != null)
            {
                errors.Add($"Línea {i + 2}: {error}");
                continue;
            }

            Guid? folderId = string.IsNullOrWhiteSpace(row.FolderPath)
                ? null
                : await ResolveOrCreateFolderPathAsync(row.FolderPath, allFolders, cancellationToken);

            var outcome = await ProcessImportRowAsync(row, folderId, resolutionOverrides, defaultResolution, cancellationToken);
            switch (outcome)
            {
                case ImportRowOutcome.Imported: imported++; break;
                case ImportRowOutcome.Overwritten: overwritten++; break;
                case ImportRowOutcome.Duplicated: duplicated++; break;
                case ImportRowOutcome.Skipped: skipped++; break;
            }
        }

        return new CsvImportResult(
            TotalProcessed: rows.Count,
            ImportedCount: imported,
            OverwrittenCount: overwritten,
            DuplicatedCount: duplicated,
            SkippedCount: skipped,
            Errors: errors);
    }

    private enum ImportRowOutcome { Imported, Overwritten, Duplicated, Skipped }

    private async Task<ImportRowOutcome> ProcessImportRowAsync(
        CsvSnippetRow row,
        Guid? folderId,
        IReadOnlyDictionary<string, CsvConflictResolution>? resolutionOverrides,
        CsvConflictResolution defaultResolution,
        CancellationToken cancellationToken)
    {
        var existingSnippet = await _snippetRepository.GetByCodeAsync(row.Code.Trim(), cancellationToken);
        if (existingSnippet == null)
        {
            await CreateNewSnippetAsync(row, folderId, cancellationToken);
            return ImportRowOutcome.Imported;
        }

        var resolution = resolutionOverrides != null && resolutionOverrides.TryGetValue(row.Code.Trim(), out var customRes)
            ? customRes
            : defaultResolution;

        return await ApplyConflictResolutionAsync(existingSnippet, row, folderId, resolution, cancellationToken);
    }

    private async Task<ImportRowOutcome> ApplyConflictResolutionAsync(
        Snippet existingSnippet,
        CsvSnippetRow row,
        Guid? folderId,
        CsvConflictResolution resolution,
        CancellationToken cancellationToken)
    {
        switch (resolution)
        {
            case CsvConflictResolution.Skip:
                return ImportRowOutcome.Skipped;

            case CsvConflictResolution.Overwrite:
                await OverwriteSnippetAsync(existingSnippet, row, folderId, cancellationToken);
                return ImportRowOutcome.Overwritten;

            case CsvConflictResolution.DuplicateWithSuffix:
                await DuplicateSnippetWithSuffixAsync(row, folderId, cancellationToken);
                return ImportRowOutcome.Duplicated;

            default:
                return ImportRowOutcome.Skipped;
        }
    }

    private async Task OverwriteSnippetAsync(Snippet existingSnippet, CsvSnippetRow row, Guid? folderId, CancellationToken cancellationToken)
    {
        existingSnippet.Update(
            row.Code.Trim(),
            string.IsNullOrWhiteSpace(row.Name) ? row.Code.Trim() : row.Name.Trim(),
            row.Content,
            folderId,
            row.CaseSensitive);

        if (row.IsActive != existingSnippet.IsActive)
        {
            existingSnippet.ToggleActive();
        }
        if (row.IsFavorite != existingSnippet.IsFavorite)
        {
            existingSnippet.ToggleFavorite();
        }

        await _snippetRepository.UpdateAsync(existingSnippet, cancellationToken);
    }

    private async Task DuplicateSnippetWithSuffixAsync(CsvSnippetRow row, Guid? folderId, CancellationToken cancellationToken)
    {
        var uniqueCode = await GenerateUniqueCodeAsync(row.Code.Trim(), cancellationToken);
        var duplicateSnippet = Snippet.Create(
            uniqueCode,
            string.IsNullOrWhiteSpace(row.Name) ? uniqueCode : row.Name.Trim(),
            row.Content,
            folderId,
            row.CaseSensitive);

        if (!row.IsActive)
        {
            duplicateSnippet.ToggleActive();
        }
        if (row.IsFavorite)
        {
            duplicateSnippet.ToggleFavorite();
        }

        await _snippetRepository.AddAsync(duplicateSnippet, cancellationToken);
    }

    private async Task CreateNewSnippetAsync(CsvSnippetRow row, Guid? folderId, CancellationToken cancellationToken)
    {
        var newSnippet = Snippet.Create(
            row.Code.Trim(),
            string.IsNullOrWhiteSpace(row.Name) ? row.Code.Trim() : row.Name.Trim(),
            row.Content,
            folderId,
            row.CaseSensitive);

        if (!row.IsActive)
        {
            newSnippet.ToggleActive();
        }
        if (row.IsFavorite)
        {
            newSnippet.ToggleFavorite();
        }

        await _snippetRepository.AddAsync(newSnippet, cancellationToken);
    }

    private static string? ValidateRow(CsvSnippetRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Code))
        {
            return "El código del atajo es obligatorio.";
        }
        if (row.Code.Trim().Length > Snippet.MaxCodeLength)
        {
            return $"El código del atajo excede los {Snippet.MaxCodeLength} caracteres.";
        }
        if (string.IsNullOrEmpty(row.Content))
        {
            return "El contenido del atajo no puede estar vacío.";
        }
        if (row.Content.Length > Snippet.MaxContentLength)
        {
            return $"El contenido excede los {Snippet.MaxContentLength} caracteres permitidos.";
        }
        return null;
    }

    private async Task<Guid?> ResolveOrCreateFolderPathAsync(string path, List<Folder> existingFolders, CancellationToken cancellationToken)
    {
        var segments = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            return null;
        }

        // Limitar a la profundidad máxima admitida por el dominio (3 niveles)
        int maxDepth = Math.Min(segments.Length, Folder.MaxHierarchyDepth);
        Guid? parentId = null;

        for (int depth = 0; depth < maxDepth; depth++)
        {
            string segmentName = segments[depth];
            var folder = existingFolders.FirstOrDefault(f => f.ParentId == parentId && string.Equals(f.Name, segmentName, StringComparison.OrdinalIgnoreCase));

            if (folder == null)
            {
                folder = Folder.Create(segmentName, parentId);
                await _folderRepository.AddAsync(folder, cancellationToken);
                existingFolders.Add(folder);
            }

            parentId = folder.Id;
        }

        return parentId;
    }

    private async Task<string> GenerateUniqueCodeAsync(string baseCode, CancellationToken cancellationToken)
    {
        int counter = 1;
        while (true)
        {
            string candidate = $"{baseCode}_{counter}";
            if (candidate.Length > Snippet.MaxCodeLength)
            {
                candidate = candidate.Substring(0, Snippet.MaxCodeLength);
            }

            bool exists = await _snippetRepository.ExistsCodeAsync(candidate, null, cancellationToken);
            if (!exists)
            {
                return candidate;
            }

            counter++;
        }
    }

    private async Task<Dictionary<Guid, string>> BuildFolderPathMapAsync(CancellationToken cancellationToken)
    {
        var folders = await _folderRepository.GetAllAsync(cancellationToken);
        return BuildFolderPathMap(folders);
    }

    private static Dictionary<Guid, string> BuildFolderPathMap(IReadOnlyList<Folder> folders)
    {
        var folderById = folders.ToDictionary(f => f.Id);
        var map = new Dictionary<Guid, string>();

        foreach (var folder in folders)
        {
            map[folder.Id] = GetFullPath(folder, folderById);
        }

        return map;
    }

    private static string GetFullPath(Folder folder, Dictionary<Guid, Folder> folderById)
    {
        var parts = new List<string> { folder.Name };
        var current = folder;

        while (current.ParentId.HasValue && folderById.TryGetValue(current.ParentId.Value, out var parent))
        {
            parts.Insert(0, parent.Name);
            current = parent;
        }

        return string.Join("/", parts);
    }

    private static CsvSnippetRow MapToRow(Snippet snippet, Dictionary<Guid, string> folderPathMap)
    {
        string folderPath = "";
        if (snippet.FolderId.HasValue && folderPathMap.TryGetValue(snippet.FolderId.Value, out var path))
        {
            folderPath = path;
        }

        return new CsvSnippetRow(
            Code: snippet.Code,
            Name: snippet.Name,
            Content: snippet.Content,
            FolderPath: folderPath,
            CaseSensitive: snippet.CaseSensitive,
            IsActive: snippet.IsActive,
            IsFavorite: snippet.IsFavorite,
            SortOrder: snippet.SortOrder);
    }
}
