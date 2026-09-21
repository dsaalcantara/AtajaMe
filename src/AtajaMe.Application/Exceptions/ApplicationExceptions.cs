namespace AtajaMe.Application.Exceptions;

public class SnippetNotFoundException : Exception
{
    public Guid SnippetId { get; }

    public SnippetNotFoundException(Guid snippetId)
        : base($"No se encontró ningún atajo con el identificador '{snippetId}'.")
    {
        SnippetId = snippetId;
    }
}

public class DuplicateSnippetCodeException : Exception
{
    public string Code { get; }

    public DuplicateSnippetCodeException(string code)
        : base($"Ya existe un atajo registrado con el código '{code}'.")
    {
        Code = code;
    }
}

public class FolderNotFoundException : Exception
{
    public Guid FolderId { get; }

    public FolderNotFoundException(Guid folderId)
        : base($"No se encontró ninguna carpeta con el identificador '{folderId}'.")
    {
        FolderId = folderId;
    }
}

public class MaxFolderDepthExceededException : Exception
{
    public int MaxDepth { get; }

    public MaxFolderDepthExceededException(int maxDepth)
        : base($"La jerarquía de carpetas no puede superar los {maxDepth} niveles de profundidad.")
    {
        MaxDepth = maxDepth;
    }
}

public class FolderCycleException : Exception
{
    public FolderCycleException(string message) : base(message)
    {
    }
}
