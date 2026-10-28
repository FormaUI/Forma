namespace Forma.Framework.Culture.Router.AspNetCore;

using Microsoft.AspNetCore.Components;

/// <summary>The base of a page carrying its culture route beside its plain one, <c>@page "/{culture}/about"</c> beside <c>@page "/about"</c>.</summary>
/// <remarks>
/// The parameters are assigned before the page's lifecycle, and the culture segment they carry is followed there: a
/// segment that isn't a culture's token stops the page with the router's not-found outcome, and a page an interactive
/// router renders within <see cref="CultureRouteScope"/> under <see cref="CultureSwitchMode.InPlace"/> brings the culture state
/// to its route's culture before its initialization and parameter callbacks run, or, at an address that isn't its
/// culture's canonical one, replaces the address and stops there.
/// </remarks>
public abstract class CultureRoutePage : ComponentBase
{
    /// <summary>The culture segment the page was routed with.</summary>
    [Parameter] public string? Culture { get; set; }

    [CascadingParameter] private CultureRouteScope? CultureRouteScope { get; set; }

    [Inject] private CultureSwitcher CultureSwitcher { get; set; } = default!;

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        return CultureSwitcher.FollowRoute(Culture, RendererInfo.IsInteractive && CultureRouteScope is not null)
            ? base.SetParametersAsync(ParameterView.Empty)
            : Task.CompletedTask;
    }
}
