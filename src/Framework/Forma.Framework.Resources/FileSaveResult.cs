namespace Forma.Framework.Resources;

using System.Diagnostics.CodeAnalysis;

/// <summary>What a store made of a file: the resource it saved the file as, or its refusal of the file.</summary>
public sealed record FileSaveResult
{
    private FileSaveResult(ResourceInfo? resource, FileRefusal? refusal)
    {
        Resource = resource;
        Refusal = refusal;
    }

    /// <summary>The resource the store saved the file as.</summary>
    public ResourceInfo? Resource { get; }

    /// <summary>The store's refusal of the file.</summary>
    public FileRefusal? Refusal { get; }

    /// <summary>Whether the store saved the file.</summary>
    [MemberNotNullWhen(true, nameof(Resource))]
    [MemberNotNullWhen(false, nameof(Refusal))]
    public bool IsSaved
        => Resource is not null;

    /// <summary>The result of a file the store saved as <paramref name="resource"/>.</summary>
    /// <param name="resource">The resource the store saved the file as.</param>
    public static FileSaveResult Saved(ResourceInfo resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return new(resource, null);
    }

    /// <summary>The result of a file the store refused.</summary>
    /// <param name="refusal">The store's refusal of the file.</param>
    public static FileSaveResult Refused(FileRefusal refusal)
    {
        ArgumentNullException.ThrowIfNull(refusal);

        return new(null, refusal);
    }
}
