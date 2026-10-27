namespace Forma.Framework.Culture.Router.AspNetCore;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;

/// <summary>The document's <c>&lt;base&gt;</c> element in the request's culture, <c>/app/he/</c> under the path base <c>/app</c>.</summary>
/// <remarks>
/// Interactive routing finds a page within the base, so it finds the pages of the document's culture and none at an
/// address in another culture, which loads as a new document: outside the base, or within the default culture's base
/// under <see cref="CulturePrefix.ExceptDefault"/>, which carries no culture segment.
/// </remarks>
public sealed class CultureBaseHref : ComponentBase
{
    [CascadingParameter] private HttpContext? HttpContext { get; set; }

    [Inject] private CultureRouter CultureRouter { get; set; } = default!;

    [Inject] private CultureSwitcher CultureSwitcher { get; set; } = default!;

    private string Href
        => (HttpContext?.Request.PathBase ?? PathString.Empty).Add(CultureRouter.GetPath(CultureSwitcher.CurrentRoute, "/")).ToUriComponent().TrimEnd('/') + "/";

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.OpenElement(0, "base");
        builder.AddAttribute(1, "href", Href);
        builder.CloseElement();
    }
}
