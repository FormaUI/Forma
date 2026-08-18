namespace Forma.UI.Blazor.Components.Gallery;

using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>Renders the gallery's operations for one media resource as an overflow menu.</summary>
public sealed partial class FormaGalleryActionBar : ComponentBase
{
    /// <summary>The media resource the actions run against.</summary>
    [Parameter, EditorRequired] public ResourceInfo Resource { get; set; } = default!;

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
