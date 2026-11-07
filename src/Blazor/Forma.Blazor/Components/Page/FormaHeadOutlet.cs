namespace Forma.Blazor.Components.Page;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

/// <summary>The framework's head outlet, writing the document's one title: the one a page sets, or <see cref="Title"/> where it sets none.</summary>
/// <remarks>
/// <see cref="Title"/> is a page title of its own, rendered ahead of the page in the outlet's runtime, and the framework writes
/// the title set last; so a title a page or its layout sets replaces it, and it returns once that title leaves. Rendered in the
/// page's render mode, the outlet runs where the page's own title is set.
/// </remarks>
internal sealed class FormaHeadOutlet : FormaComponentBase
{
    /// <summary>The title a page takes where it sets none of its own.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentException.ThrowIfNullOrWhiteSpace(Title);

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.OpenComponent<PageTitle>(0);
        builder.AddComponentParameter(1, nameof(PageTitle.ChildContent), (RenderFragment)(title => title.AddContent(0, Title)));
        builder.CloseComponent();
        builder.OpenComponent<HeadOutlet>(2);
        builder.CloseComponent();
    }
}
