namespace Forma.Framework.Culture.Router;

using Microsoft.AspNetCore.Components;

/// <summary>The base of a page carrying its culture route beside its plain one, <c>@page "/{culture}/about"</c> beside <c>@page "/about"</c>.</summary>
/// <remarks>
/// The parameters are assigned before the page's lifecycle, and the culture segment they carry is followed there: a
/// segment that isn't a culture's token stops the page with the router's not-found outcome, and a page an interactive
/// router renders within <see cref="CultureRouteScope"/> under <see cref="CultureSwitchMode.InPlace"/> reports its landing to
/// the page's culture coordinator and runs its initialization and parameter callbacks once its culture state has applied
/// the route's pair, or, at an address that isn't its route's canonical one, replaces the address and stops there.
/// </remarks>
public abstract class CultureRoutePage : ComponentBase
{
    /// <summary>The culture segment the page was routed with.</summary>
    [Parameter] public string? Culture { get; set; }

    [CascadingParameter] private CultureRouteScope? CultureRouteScope { get; set; }

    [Inject] private CultureSwitcher CultureSwitcher { get; set; } = default!;

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (await CultureSwitcher.FollowRouteAsync(Culture, RendererInfo.IsInteractive && CultureRouteScope is not null))
        {
            CultureSwitcher.HoldStateCultures();
            await base.SetParametersAsync(ParameterView.Empty);
        }
    }
}
