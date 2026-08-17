namespace Forma.UI.Components.Gallery;

using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>Thumbnail grid view for gallery media resources.</summary>
public sealed partial class FormaGalleryThumbnailsView : ComponentBase
{
    /// <summary>The collection of media resources to display.</summary>
    [Parameter] public IReadOnlyList<ResourceInfo> Resources { get; set; } = [];

    [CascadingParameter] private GalleryContext Context { get; set; } = default!;

    protected override void OnInitialized()
    {
        if (Context is null)
        {
            throw new InvalidOperationException($"{nameof(FormaGalleryThumbnailsView)} must be rendered inside a {nameof(FormaGallery)}.");
        }
    }
}
