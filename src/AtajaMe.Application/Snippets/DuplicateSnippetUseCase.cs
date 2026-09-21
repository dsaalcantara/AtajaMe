using AtajaMe.Application.DTOs;
using AtajaMe.Application.Exceptions;
using AtajaMe.Domain.Repositories;

namespace AtajaMe.Application.Snippets;

/// <summary>
/// Caso de uso para duplicar un atajo existente asignándole un nuevo código único.
/// </summary>
public sealed class DuplicateSnippetUseCase
{
    private readonly ISnippetRepository _snippetRepository;

    public DuplicateSnippetUseCase(ISnippetRepository snippetRepository)
    {
        _snippetRepository = snippetRepository;
    }

    public async Task<SnippetDto> ExecuteAsync(Guid snippetId, string? requestedCode = null, CancellationToken cancellationToken = default)
    {
        var snippet = await _snippetRepository.GetByIdAsync(snippetId, cancellationToken);
        if (snippet is null)
        {
            throw new SnippetNotFoundException(snippetId);
        }

        string newCode;
        if (!string.IsNullOrWhiteSpace(requestedCode))
        {
            newCode = requestedCode.Trim();
            if (await _snippetRepository.ExistsCodeAsync(newCode, null, cancellationToken))
            {
                throw new DuplicateSnippetCodeException(newCode);
            }
        }
        else
        {
            // Generar un código derivado único automáticamente (ej. "bd_copia", "bd_copia2", ...)
            var baseCode = snippet.Code;
            var suffixIndex = 1;
            string? uniqueCandidate = null;

            while (suffixIndex < 1000)
            {
                var suffix = suffixIndex == 1 ? "_copia" : $"_copia{suffixIndex}";
                var maxBaseLength = Domain.Entities.Snippet.MaxCodeLength - suffix.Length;
                var safeBase = baseCode.Length > maxBaseLength ? baseCode[..Math.Max(1, maxBaseLength)] : baseCode;
                var candidate = $"{safeBase}{suffix}";

                if (!await _snippetRepository.ExistsCodeAsync(candidate, null, cancellationToken))
                {
                    uniqueCandidate = candidate;
                    break;
                }

                suffixIndex++;
            }

            newCode = uniqueCandidate ?? $"{Guid.NewGuid().ToString("N")[..8]}";
        }

        var duplicate = snippet.Duplicate(newCode);
        await _snippetRepository.AddAsync(duplicate, cancellationToken);
        return SnippetDto.FromDomain(duplicate);
    }
}
