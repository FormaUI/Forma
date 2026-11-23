namespace Forma.Blazor.Components.Navigation;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components.Routing;

/// <summary>One tab of a <see cref="FormaLinkTabs"/>, linking to a page.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
[SuppressMessage("Design", "CA1056:URI properties should not be strings")]
public sealed record LinkTab
{
    /// <summary>The tab's title, shown in the strip.</summary>
    public string Title { get; }

    /// <summary>The address the tab links to.</summary>
    public string Url { get; }

    /// <summary>The icon shown before the title.</summary>
    public Icon? Icon { get; init; }

    /// <summary>How the address of the page the reader is on matches the tab's.</summary>
    public NavLinkMatch Match { get; init; } = NavLinkMatch.Prefix;

    /// <summary>Whether the tab sits at the far end of the strip, after every leading tab.</summary>
    public bool Trailing { get; init; }

    /// <summary>Creates a tab from its title and the address it links to.</summary>
    public LinkTab(string title, string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(url);
        Title = title;
        Url = url;
    }
}
