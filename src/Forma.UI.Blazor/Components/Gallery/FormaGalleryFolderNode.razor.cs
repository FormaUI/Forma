namespace Forma.UI.Blazor.Components.Gallery;

using Forma.Framework.Localization;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>One folder in the sidebar tree, rendering its own descendants.</summary>
public sealed partial class FormaGalleryFolderNode : ComponentBase
{
    /// <summary>The folder this node represents.</summary>
    [Parameter, EditorRequired] public FolderResourceInfo Folder { get; set; } = default!;

    /// <summary>How many levels below the root this node sits, driving its indent.</summary>
    [Parameter] public int Depth { get; set; }

    [CascadingParameter] private FormaGallerySidebar Sidebar { get; set; } = default!;

    [CascadingParameter] private GalleryContext Context { get; set; } = default!;

    [Inject] private IStringLocale<FormaGallery> L { get; set; } = default!;

    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Folder);
        ArgumentOutOfRangeException.ThrowIfNegative(Depth);

        if (Sidebar is null)
        {
            throw new InvalidOperationException($"{nameof(FormaGalleryFolderNode)} must be rendered inside a {nameof(FormaGallerySidebar)}.");
        }
    }
}
