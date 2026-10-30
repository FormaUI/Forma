namespace Forma.Blazor.Components.Page;

using System.Globalization;

using Forma.Framework.Culture.Browser.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>The document head a Forma host needs: its character set, viewport, title, base address, the culture marker and switch policy the page's culture coordinator reads, Forma's own stylesheets, and the framework's asset and head outlets.</summary>
public sealed partial class FormaPageHead : FormaComponentBase
{
    /// <summary>The document title.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    /// <summary>The base address every relative URL resolves against.</summary>
    [Parameter] public string BaseHref { get; set; } = "/";

    /// <summary>The address of the icon a browser shows for the page in its tab and bookmarks.</summary>
    [Parameter] public string? IconHref { get; set; }

    /// <summary>The head content rendered after Forma's stylesheets.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Whether the runtime assets a WebAssembly render mode needs are preloaded.</summary>
    [Parameter] public bool PreloadAssets { get; set; } = true;

    [Inject] private CultureSwitchPolicy Policy { get; set; } = default!;

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
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);
        ArgumentException.ThrowIfNullOrWhiteSpace(BaseHref);
    }
}
