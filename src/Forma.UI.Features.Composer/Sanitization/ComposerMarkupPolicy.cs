namespace Forma.UI.Features.Composer.Sanitization;

/// <summary>The markup the built-in plugins produce, and nothing else.</summary>
public sealed class ComposerMarkupPolicy : IMarkupPolicy
{
    /// <summary>The policy a sanitizer applies when a host names none.</summary>
    public static ComposerMarkupPolicy Default { get; } = new();

    private static readonly HashSet<string> Tags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "h1", "h2", "h3", "h4", "h5", "h6", "blockquote", "pre", "code",
        "b", "strong", "i", "em", "u", "s", "strike", "sub", "sup", "mark", "small",
        "ul", "ol", "li",
        "a", "img", "video", "source", "audio",
        "table", "thead", "tbody", "tfoot", "tr", "th", "td", "caption", "colgroup", "col",
        "hr", "br",
        "span", "div",
        "details", "summary", "figure", "figcaption"
    };

    private static readonly HashSet<string> DiscardedWithContent = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "iframe", "object", "svg", "math", "noscript", "template", "applet", "frameset"
    };

    private static readonly HashSet<string> GlobalAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "class", "id", "style", "dir", "title", "lang"
    };

    private static readonly Dictionary<string, HashSet<string>> TagAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a"] = new(StringComparer.OrdinalIgnoreCase) { "href", "target", "rel", "download", "name" },
        ["img"] = new(StringComparer.OrdinalIgnoreCase) { "src", "alt", "width", "height", "loading" },
        ["video"] = new(StringComparer.OrdinalIgnoreCase) { "src", "poster", "controls", "autoplay", "loop", "muted", "preload", "width", "height" },
        ["audio"] = new(StringComparer.OrdinalIgnoreCase) { "src", "controls", "autoplay", "loop", "muted", "preload" },
        ["source"] = new(StringComparer.OrdinalIgnoreCase) { "src", "type" },
        ["th"] = new(StringComparer.OrdinalIgnoreCase) { "colspan", "rowspan", "scope", "headers", "align" },
        ["td"] = new(StringComparer.OrdinalIgnoreCase) { "colspan", "rowspan", "headers", "align" },
        ["table"] = new(StringComparer.OrdinalIgnoreCase) { "border", "cellpadding", "cellspacing" },
        ["ol"] = new(StringComparer.OrdinalIgnoreCase) { "start", "type", "reversed" },
        ["li"] = new(StringComparer.OrdinalIgnoreCase) { "value" }
    };

    private static readonly HashSet<string> StyleProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "color", "background-color", "font-family", "font-size", "font-weight", "font-style",
        "text-align", "text-decoration", "line-height",
        "margin", "margin-block", "margin-block-start", "margin-block-end",
        "margin-inline", "margin-inline-start", "margin-inline-end",
        "padding", "padding-block", "padding-block-start", "padding-block-end",
        "padding-inline", "padding-inline-start", "padding-inline-end",
        "border", "border-color", "border-width", "border-style",
        "width", "height", "max-width", "max-height", "min-width", "min-height"
    };

    private static readonly string[] StyleDenyList =
    [
        "url(", "javascript:", "expression(", "@import", "behavior:"
    ];

    private static readonly string[] UrlSchemes =
    [
        "http://", "https://", "mailto:", "tel:"
    ];

    // SVG is excluded because it can carry script.
    private static readonly string[] DataUrlPrefixes =
    [
        "data:image/png", "data:image/jpeg", "data:image/jpg",
        "data:image/gif", "data:image/webp", "data:image/avif"
    ];

    // The alternate lookups match a sliced name without copying it to a string first, and hand back the
    // allowlist's own spelling, so the output is normalized without a ToLowerInvariant per name.
    private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> TagLookup = Tags.GetAlternateLookup<ReadOnlySpan<char>>();
    private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> DiscardedLookup = DiscardedWithContent.GetAlternateLookup<ReadOnlySpan<char>>();
    private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> GlobalAttributeLookup = GlobalAttributes.GetAlternateLookup<ReadOnlySpan<char>>();
    private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> StylePropertyLookup = StyleProperties.GetAlternateLookup<ReadOnlySpan<char>>();
    private static readonly Dictionary<string, HashSet<string>>.AlternateLookup<ReadOnlySpan<char>> TagAttributeLookup =
        TagAttributes.GetAlternateLookup<ReadOnlySpan<char>>();

    /// <inheritdoc />
    public bool Allows(HtmlSubject subject, ReadOnlySpan<char> value, ReadOnlySpan<char> owner, out string? canonical)
    {
        canonical = null;

        return subject switch
        {
            HtmlSubject.Element => TagLookup.TryGetValue(value, out canonical),
            HtmlSubject.ElementContent => !DiscardedLookup.Contains(value),
            HtmlSubject.Attribute => AllowsAttribute(value, owner, out canonical),
            HtmlSubject.StyleProperty => AllowsStyleProperty(value, out canonical),
            HtmlSubject.StyleDeclaration => !IsDenied(value),
            HtmlSubject.Url => IsAllowedUrl(value),
            _ => false,
        };
    }

    private static bool AllowsAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> owner, out string? canonical)
    {
        if (GlobalAttributeLookup.TryGetValue(name, out canonical))
        {
            return true;
        }

        if (TagAttributeLookup.TryGetValue(owner, out var set)
            && set.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(name, out canonical))
        {
            return true;
        }

        // A data attribute carries authoring state rather than behaviour, and keeps the spelling it was written with.
        canonical = null;

        return name.StartsWith("data-", StringComparison.OrdinalIgnoreCase);
    }

    private static bool AllowsStyleProperty(ReadOnlySpan<char> property, out string? canonical)
    {
        if (StylePropertyLookup.TryGetValue(property, out canonical))
        {
            return true;
        }

        // A Forma custom property is the theme's own, and keeps the spelling it was written with.
        canonical = null;

        return property.StartsWith("--forma-", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDenied(ReadOnlySpan<char> declaration)
    {
        foreach (var denied in StyleDenyList)
        {
            if (declaration.Contains(denied, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAllowedUrl(ReadOnlySpan<char> url)
    {
        var trimmed = url.Trim();

        if (trimmed.IsEmpty)
        {
            return false;
        }

        // A bare '#' names no fragment, so it addresses nothing.
        if (trimmed[0] == '#')
        {
            return trimmed.Length > 1;
        }

        // A path resolves against whatever origin the document is served from, so it names no scheme to judge.
        if (trimmed[0] == '/'
            || trimmed.StartsWith("./", StringComparison.Ordinal)
            || trimmed.StartsWith("../", StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var scheme in UrlSchemes)
        {
            if (trimmed.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
            {
                // A scheme with nothing behind it points nowhere, so "http://" is refused as firmly as "http".
                return trimmed.Length > scheme.Length;
            }
        }

        foreach (var prefix in DataUrlPrefixes)
        {
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                // The bytes follow a comma; without one the URL declares a type and carries no picture.
                return trimmed.IndexOf(',') > 0;
            }
        }

        return false;
    }
}
