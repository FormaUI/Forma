namespace Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;

/// <summary>A navigation manager at a given address that records each navigation asked of it.</summary>
public sealed class FakeNavigationManager : NavigationManager
{
    /// <summary>Creates the navigation manager at <paramref name="uri"/> under <paramref name="baseUri"/>.</summary>
    public FakeNavigationManager(Uri baseUri, Uri uri)
    {
        ArgumentNullException.ThrowIfNull(baseUri);
        ArgumentNullException.ThrowIfNull(uri);

        Initialize(baseUri.AbsoluteUri, uri.AbsoluteUri);
    }

    /// <summary>The navigations asked of the navigation manager, in order, each with its address and options.</summary>
    public List<(string Uri, NavigationOptions Options)> Navigations { get; } = [];

    /// <inheritdoc />
    protected override void NavigateToCore(string uri, NavigationOptions options)
        => Navigations.Add((uri, options));
}
