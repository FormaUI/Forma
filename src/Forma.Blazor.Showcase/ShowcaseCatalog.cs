namespace Forma.Blazor.Showcase;

using System.Reflection;

using Microsoft.AspNetCore.Components;

/// <summary>One page the showcase serves, and which variant of its component that page demonstrates.</summary>
/// <param name="Component">The page the variant belongs to, which is the page itself for Forma's own.</param>
/// <param name="Title">The component the page demonstrates.</param>
/// <param name="Category">The sidebar section the page sits in.</param>
/// <param name="Route">The page's own route.</param>
/// <param name="Variant">What supplies the demonstrated behaviour, such as an artwork package.</param>
public sealed record ShowcasePage(string Component, string Title, string Category, string Route, string Variant);

/// <summary>The pages the showcase serves, read once from the routed components in this assembly.</summary>
/// <remarks>
/// A page named <c>FormaEmojiPage.Twemoji.razor</c> demonstrates the same component as
/// <c>FormaEmojiPage.razor</c> through a different package, and joins that page's tab strip by
/// virtue of its name. Adding such a file is all it takes to add a tab.
/// </remarks>
public static class ShowcaseCatalog
{
    /// <summary>The variant a page demonstrates when its name carries none: the library's own defaults.</summary>
    public const string DefaultVariant = "Forma";

    private static readonly IReadOnlyList<ShowcasePage> All = Discover();

    /// <summary>The pages the sidebar lists, one per component, grouped by category.</summary>
    public static ILookup<string, ShowcasePage> Sidebar { get; } = All
        .Where(page => string.Equals(page.Variant, DefaultVariant, StringComparison.Ordinal))
        .OrderBy(page => CategoryRank(page.Category))
        .ThenBy(page => page.Category, StringComparer.Ordinal)
        .ThenBy(page => page.Title, StringComparer.Ordinal)
        .ToLookup(page => page.Category);

    /// <summary>The variants of whichever page <paramref name="path"/> names, Forma's own first.</summary>
    /// <param name="path">A route, already stripped of any culture segment.</param>
    /// <returns>The page's variants, or nothing when the path names no page or the page has only one.</returns>
    public static IReadOnlyList<ShowcasePage> Variants(string path)
    {
        var current = All.FirstOrDefault(page => IsRoute(page, path));

        if (current is null)
        {
            return [];
        }

        var variants = All
            .Where(page => string.Equals(page.Component, current.Component, StringComparison.Ordinal))
            .OrderBy(page => string.Equals(page.Variant, DefaultVariant, StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(page => page.Variant, StringComparer.Ordinal)
            .ToArray();

        return variants.Length > 1 ? variants : [];
    }

    private static bool IsRoute(ShowcasePage page, string path)
        => string.Equals(page.Route.TrimEnd('/'), path.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);

    private static List<ShowcasePage> Discover()
    {
        var routed = typeof(Program).Assembly.GetTypes()
            .Select(type => (Type: type, Route: type.GetCustomAttribute<RouteAttribute>()?.Template))
            .Where(entry => entry.Route is not null)
            .ToArray();

        // Only a page carrying the attribute names a component; a variant takes its title from the page it varies.
        var titles = routed
            .Select(entry => (entry.Type.Name, Attribute: entry.Type.GetCustomAttribute<ShowcasePageAttribute>()))
            .Where(entry => entry.Attribute is not null)
            .ToDictionary(entry => entry.Name, entry => entry.Attribute!.Title, StringComparer.Ordinal);

        var pages = new List<ShowcasePage>();

        foreach (var (type, route) in routed)
        {
            // Razor compiles a dot in a file name to an underscore, so FormaEmojiPage.Twemoji.razor arrives here
            // as FormaEmojiPage_Twemoji and splits back into the page it varies and the variant it demonstrates.
            var separator = type.Name.IndexOf('_', StringComparison.Ordinal);
            var component = separator < 0 ? type.Name : type.Name[..separator];
            var variant = separator < 0 ? DefaultVariant : type.Name[(separator + 1)..];

            if (titles.TryGetValue(component, out var title))
            {
                pages.Add(new ShowcasePage(component, title, Category(type), route!, variant));
            }
        }

        return pages;
    }

    private static string Category(Type type)
    {
        var space = type.Namespace ?? string.Empty;

        return space.Contains('.', StringComparison.Ordinal) ? space[(space.LastIndexOf('.', StringComparison.Ordinal) + 1)..] : space;
    }

    // Symbols leads as the library's own vocabulary, Features and Libraries group the composite demos,
    // and everything else follows alphabetically.
    private static int CategoryRank(string category)
        => category switch
        {
            "Symbols" => 0,
            "Features" => 1,
            "Libraries" => 2,
            _ => 3,
        };
}
