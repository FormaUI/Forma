namespace Forma.Blazor.Components.Page;

using Forma.Framework.Culture.Browser.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

/// <summary>The document head a Forma host needs: its character set, viewport, title, base address, the culture marker and switch policy the page's culture coordinator reads, Forma's own stylesheets, and the framework's asset and head outlets.</summary>
/// <remarks>
/// <para>
/// The head outlet writes the document's one title, the one a page sets or <see cref="Title"/> where it sets none, and runs
/// in <see cref="PageRenderMode"/>, so a page's title and head content follow the page wherever it renders.
/// </para>
/// <para>
/// Where culture routing is registered, the page's base is the one routing gives the page's pair and the policy is the
/// address policy, publishing routing's mapping; otherwise the base is the host's, or the application's root where the
/// host gives none.
/// </para>
/// </remarks>
public sealed partial class FormaPageHead : FormaComponentBase
{
    private ICultureAddressing? _addressing;
    private CultureRouteMap? _map;

    /// <summary>The document's title where a page sets none of its own.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    /// <summary>The render mode the page's content renders in, which the head outlet takes too.</summary>
    [Parameter] public IComponentRenderMode? PageRenderMode { get; set; }

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

    private IReadOnlyDictionary<string, object> MarkerAttributes
        => ToAttributes(CulturePublication.GetMarkerAttributes(Context.CultureState.Culture, Context.CultureState.UICulture));

    private IReadOnlyDictionary<string, object> PolicyAttributes
        => ToAttributes(CulturePublication.GetPolicyAttributes(Policy, _map));

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

    // The head outlet with the title, in the page's render mode.
    private RenderFragment RenderHeadOutlet() => builder =>
    {
        builder.OpenComponent<FormaHeadOutlet>(0);
        builder.AddComponentParameter(1, nameof(FormaHeadOutlet.Title), Title);
        builder.AddComponentRenderMode(PageRenderMode);
        builder.CloseComponent();
    };

    private static Dictionary<string, object> ToAttributes(IReadOnlyDictionary<string, string> attributes)
        => attributes.ToDictionary(attribute => attribute.Key, attribute => (object)attribute.Value);
}
