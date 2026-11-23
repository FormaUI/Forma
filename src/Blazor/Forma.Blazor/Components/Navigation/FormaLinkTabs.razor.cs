namespace Forma.Blazor.Components.Navigation;

using Forma.Blazor.Components.Layout;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

/// <summary>A strip of tabs linking to pages, in a navigation landmark, with the tab of the page the reader is on marked current.</summary>
/// <remarks>
/// Addresses are compared without their query and fragment, ignoring case. A tab is current where the page's address is the tab's,
/// or, for a tab matching by prefix, lies beneath it; of several such tabs, the one whose address is longest is current.
/// </remarks>
public sealed partial class FormaLinkTabs : FormaElementBase
{
    /// <summary>The tabs of the strip, in order.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<LinkTab> Items { get; set; } = [];

    /// <summary>The accessible name of the strip's navigation landmark.</summary>
    [Parameter] public string? Label { get; set; }

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private int _currentIndex = -1;
    private int _firstTrailingIndex = -1;

    private FreezePaneOptions StripOptions
        => FreezePaneOptions.Arrows with
        {
            ScrollBackLabel = L["Scroll Tabs Back"],
            ScrollForwardLabel = L["Scroll Tabs Forward"],
            CurrentSelector = "[aria-current=page]",
        };

    /// <inheritdoc />
    protected override void OnInitialized()
        => Navigation.LocationChanged += HandleLocationChanged;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Items);

        _currentIndex = FindCurrentIndex(Navigation.Uri);
        _firstTrailingIndex = FindFirstTrailingIndex();
    }

    // The navigation manager reports a location change outside the renderer, as an enhanced navigation does.
    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
        => _ = InvokeAsync(() =>
        {
            if (IsDisposed)
            {
                return;
            }

            _currentIndex = FindCurrentIndex(args.Location);
            StateHasChanged();
        });

    private int FindCurrentIndex(string location)
    {
        var pageAddress = GetPageAddress(new Uri(location));
        var currentIndex = -1;
        var currentAddressLength = -1;

        for (var index = 0; index < Items.Count; index++)
        {
            var tab = Items[index];
            var tabAddress = GetPageAddress(Navigation.ToAbsoluteUri(tab.Url));

            if (tabAddress.Length > currentAddressLength && IsMatch(pageAddress, tabAddress, tab.Match))
            {
                currentIndex = index;
                currentAddressLength = tabAddress.Length;
            }
        }

        return currentIndex;
    }

    private int FindFirstTrailingIndex()
    {
        for (var index = 0; index < Items.Count; index++)
        {
            if (Items[index].IsTrailing)
            {
                return index;
            }
        }

        return -1;
    }

    // An address's scheme, host and path, without a trailing separator, so an address compares alike with one or without.
    private static string GetPageAddress(Uri address)
        => address.GetLeftPart(UriPartial.Path).TrimEnd('/');

    private static bool IsMatch(string pageAddress, string tabAddress, NavLinkMatch match)
        => string.Equals(pageAddress, tabAddress, StringComparison.OrdinalIgnoreCase)
            || (match == NavLinkMatch.Prefix
                && pageAddress.Length > tabAddress.Length
                && pageAddress[tabAddress.Length] == '/'
                && pageAddress.StartsWith(tabAddress, StringComparison.OrdinalIgnoreCase));

    // The first trailing tab pushes itself and those after it to the strip's far end.
    private string GetItemClass(int index)
        => new CssClassBuilder()
            .Add("forma-link-tab-item")
            .Add("forma-link-tab-item-trailing", index == _firstTrailingIndex)
            .ToStringOrNull()!;

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-link-tabs");

    /// <inheritdoc />
    protected override ValueTask DisposeAsyncCore()
    {
        Navigation.LocationChanged -= HandleLocationChanged;
        return base.DisposeAsyncCore();
    }
}
