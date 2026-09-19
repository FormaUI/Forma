namespace Forma.Framework.Explorer.Content;

using Forma.Framework.Catalog.Queries;

using Forma.Framework.Resources;

/// <summary>The resource currently shown full-size, and the siblings a viewer can step through.</summary>
public sealed class ExplorerPreview
{
    /// <summary>The resource being shown, or null when no preview is open.</summary>
    public ResourceInfo? Resource { get; private set; }

    /// <summary>The sources a viewer can step to while this preview stays open.</summary>
    public IReadOnlyList<string> Siblings { get; private set; } = [];

    /// <summary>Opens the preview on the given resource, among the given siblings.</summary>
    public void Open(ResourceInfo resource, IReadOnlyList<string> siblings)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(siblings);

        Resource = resource;
        Siblings = siblings;
    }

    /// <summary>Steps the open preview to a different resource, keeping the same siblings.</summary>
    public void NavigateTo(ResourceInfo resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        Resource = resource;
    }

    /// <summary>Closes the preview.</summary>
    public void Close()
        => Resource = null;
}
