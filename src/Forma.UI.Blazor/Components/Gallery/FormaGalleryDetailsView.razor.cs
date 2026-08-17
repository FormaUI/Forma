namespace Forma.UI.Components.Gallery;

using System.Globalization;

using Forma.Framework.Localization;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>Tabular details view for gallery media items with sortable column headers.</summary>
public sealed partial class FormaGalleryDetailsView : ComponentBase
{
    /// <summary>The collection of media items to display.</summary>
    [Parameter] public IReadOnlyList<ResourceInfo> Entries { get; set; } = [];

    [CascadingParameter] private GalleryContext Context { get; set; } = default!;

    [Inject] private IStringLocale<FormaGallery> L { get; set; } = default!;

    private const string Placeholder = "—";

    private bool ShowsKind
        => Context.Options.DetailsColumns.HasFlag(GalleryDetailsColumns.Kind);

    private bool ShowsSize
        => Context.Options.DetailsColumns.HasFlag(GalleryDetailsColumns.Size);

    private bool ShowsModified
        => Context.Options.DetailsColumns.HasFlag(GalleryDetailsColumns.Modified);

    private bool ShowsAdded
        => Context.Options.DetailsColumns.HasFlag(GalleryDetailsColumns.Added);

    protected override void OnInitialized()
    {
        if (Context is null)
        {
            throw new InvalidOperationException($"{nameof(FormaGalleryDetailsView)} must be rendered inside a {nameof(FormaGallery)}.");
        }
    }

    private static string DisplayDate(DateTimeOffset? value)
        => value?.LocalDateTime.ToString("g", CultureInfo.CurrentCulture) ?? Placeholder;

    private string SortState(GallerySortField field)
    {
        if (Context.SortField != field)
        {
            return "none";
        }

        return Context.SortDirection == GallerySortDirection.Ascending ? "ascending" : "descending";
    }
}
