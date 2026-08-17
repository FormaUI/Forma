namespace Forma.UI.Components.Gallery;

using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>Renders the gallery's item operations as buttons for one media item.</summary>
public sealed partial class FormaGalleryActionBar : ComponentBase
{
    /// <summary>The media item the actions run against.</summary>
    [Parameter, EditorRequired] public ResourceInfo Entry { get; set; } = default!;

    /// <summary>How the action row is positioned within its view.</summary>
    [Parameter] public GalleryActionBarVariant Variant { get; set; } = GalleryActionBarVariant.Inline;

    [CascadingParameter] private GalleryContext Context { get; set; } = default!;

    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Entry);

        if (Context is null)
        {
            throw new InvalidOperationException($"{nameof(FormaGalleryActionBar)} must be rendered inside a {nameof(FormaGallery)}.");
        }
    }
}
