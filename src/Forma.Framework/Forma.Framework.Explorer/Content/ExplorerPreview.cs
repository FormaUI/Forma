namespace Forma.Framework.Explorer.Content;

using Forma.Framework.Catalog.Queries;

using Forma.Framework.Resources;

/// <summary>The resource shown full-size, and the siblings a viewer can step through.</summary>
public sealed class ExplorerPreview
{
    /// <summary>The resource the preview shows.</summary>
    public ResourceInfo? Resource { get; private set; }

    /// <summary>The sources of the siblings a viewer can step through.</summary>
    public IReadOnlyList<string> Siblings { get; private set; } = [];

    /// <summary>Opens the preview on the resource, among the siblings.</summary>
    public void Open(ResourceInfo resource, IReadOnlyList<string> siblings)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(siblings);

        Resource = resource;
        Siblings = siblings;
    }

    /// <summary>Steps the preview to the resource.</summary>
    public void NavigateTo(ResourceInfo resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        Resource = resource;
    }

    /// <summary>Closes the preview.</summary>
    public void Close()
        => Resource = null;
}
