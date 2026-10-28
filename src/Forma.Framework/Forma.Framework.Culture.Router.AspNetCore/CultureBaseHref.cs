namespace Forma.Framework.Culture.Router.AspNetCore;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;

/// <summary>The document's <c>&lt;base&gt;</c> element in the request's culture, <c>/app/he/</c> under the path base <c>/app</c>, or the application's root, <c>/app/</c>, where a switch is made in place.</summary>
/// <remarks>
/// Interactive routing finds a page within the base, so under a base in a culture it finds the pages of the document's
/// culture and none at an address in another culture, which loads as a new document: outside the base, or within the
/// default culture's base under <see cref="CulturePrefix.ExceptDefault"/>, which carries no culture segment. Under
/// <see cref="CultureSwitchMode.InPlace"/> the base is the application's root, within which every culture's address lies, and
/// pages carrying their culture route take each address in the same circuit.
/// </remarks>
public sealed class CultureBaseHref : ComponentBase
{
    [CascadingParameter] private HttpContext? HttpContext { get; set; }

    [Inject] private CultureRouter CultureRouter { get; set; } = default!;

    [Inject] private CultureSwitcher CultureSwitcher { get; set; } = default!;

    private string Href
        => (HttpContext?.Request.PathBase ?? PathString.Empty).Add(Path).ToUriComponent().TrimEnd('/') + "/";

    private PathString Path
        => CultureRouter.Switch is CultureSwitchMode.InPlace ? PathString.Empty : CultureRouter.GetPath(CultureSwitcher.CurrentRoute, "/");

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.OpenElement(0, "base");
        builder.AddAttribute(1, "href", Href);
        builder.CloseElement();
    }
}
