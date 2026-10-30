namespace Forma.Blazor.Components.Page;

using System.Globalization;
using System.Text.Json;

using Forma.Framework.Culture.Browser.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

/// <summary>The document head a Forma host needs: its character set, viewport, title, base address, the culture marker and switch policy the page's culture coordinator reads, Forma's own stylesheets, and the framework's asset and head outlets.</summary>
/// <remarks>
/// Where culture routing is registered, the page's base is the one routing gives the page's pair and the policy is the
/// address policy, publishing routing's mapping; otherwise the base is the host's, or the application's root where the
/// host gives none.
/// </remarks>
public sealed partial class FormaPageHead : FormaComponentBase
{
    private static readonly JsonSerializerOptions MapOptions = new(JsonSerializerDefaults.Web);

    private ICultureAddressing? _addressing;
    private CultureRouteMap? _map;

    /// <summary>The document title.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    /// <summary>The base address every relative URL resolves against, which culture routing gives where it is registered.</summary>
    [Parameter] public string? BaseHref { get; set; }

    /// <summary>The address of the icon a browser shows for the page in its tab and bookmarks.</summary>
    [Parameter] public string? IconHref { get; set; }

    /// <summary>The head content rendered after Forma's stylesheets.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Whether the runtime assets a WebAssembly render mode needs are preloaded.</summary>
    [Parameter] public bool PreloadAssets { get; set; } = true;

    [Inject] private CultureSwitchPolicy Policy { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

    // The application's root, which the request was served under.
    private string Root
        => new Uri(Navigation.BaseUri).AbsolutePath;

    private string DocumentBase
        => _addressing?.GetDocumentBase(Root, Context.CultureState.Culture, Context.CultureState.UICulture) ?? BaseHref ?? Root;

    private string? MapRoutes
        => _map is { } map ? JsonSerializer.Serialize(map.Routes, MapOptions) : null;

    private string? MapPrefix
        => _map?.PrefixesDefault is true ? "always" : "except-default";

    private string? MapSwitch
        => _map?.SwitchesInPlace is true ? "in-place" : "full-load";

    // The cookie's lifetime in whole seconds, as a cookie's max-age reads it.
    private string? CookieMaxAge
        => Policy.CookieLifetime is { } lifetime ? ((long)lifetime.TotalSeconds).ToString(CultureInfo.InvariantCulture) : null;

    private string CookieSameSite
        => Policy.CookieSameSite switch
        {
            CultureCookieSameSite.Strict => "strict",
            CultureCookieSameSite.None => "none",
            _ => "lax",
        };

    /// <inheritdoc />
    protected override void OnInitialized()
        => _addressing = ServiceProvider.GetService<ICultureAddressing>();

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">A base address is set where culture routing gives the page's.</exception>
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);

        if (BaseHref is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(BaseHref);

            if (_addressing is not null)
            {
                throw new InvalidOperationException($"{nameof(FormaPageHead)} takes no {nameof(BaseHref)} where culture routing is registered, since routing gives the document's base in the page's culture; leave it unset.");
            }
        }

        _map = _addressing?.GetMap(Root);
    }
}
