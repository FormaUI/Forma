namespace Forma.UI.Components.Gallery;

using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>Renders the gallery's operations as buttons for one media resource.</summary>
public sealed partial class FormaGalleryActionBar : ComponentBase
{
    /// <summary>The media resource the actions run against.</summary>
    [Parameter, EditorRequired] public ResourceInfo Resource { get; set; } = default!;

    /// <summary>How the action row is positioned within its view.</summary>
    [Parameter] public GalleryActionBarVariant Variant { get; set; } = GalleryActionBarVariant.Inline;

    [CascadingParameter] private GalleryContext Context { get; set; } = default!;

    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Resource);

        if (Context is null)
        {
            throw new InvalidOperationException($"{nameof(FormaGalleryActionBar)} must be rendered inside a {nameof(FormaGallery)}.");
        }
    }
}
