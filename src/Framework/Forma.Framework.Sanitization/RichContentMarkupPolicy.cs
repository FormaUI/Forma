namespace Forma.Framework.Sanitization;

/// <summary>A markup policy permitting rich content: formatted text, lists, links, media, tables and disclosures.</summary>
public sealed class RichContentMarkupPolicy : IMarkupPolicy
{
    /// <summary>The shared instance of the policy.</summary>
    public static RichContentMarkupPolicy Default { get; } = new();

    private static readonly HashSet<string> Tags =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "p", "h1", "h2", "h3", "h4", "h5", "h6", "blockquote", "pre", "code",
        "b", "strong", "i", "em", "u", "s", "strike", "sub", "sup", "mark", "small",
        "ul", "ol", "li",
        "a", "img", "video", "source", "audio",
        "table", "thead", "tbody", "tfoot", "tr", "th", "td", "caption", "colgroup", "col",
        "hr", "br",
        "span", "div",
        "details", "summary", "figure", "figcaption"
    ];

    private static readonly HashSet<string> DiscardedWithContent =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "script", "style", "iframe", "object", "svg", "math", "noscript", "template", "applet", "frameset"
    ];

    private static readonly HashSet<string> GlobalAttributes =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "class", "id", "style", "dir", "title", "lang"
    ];

    private static readonly Dictionary<string, HashSet<string>> TagAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a"] = [with(StringComparer.OrdinalIgnoreCase), "href", "target", "rel", "download", "name"],
        ["img"] = [with(StringComparer.OrdinalIgnoreCase), "src", "alt", "width", "height", "loading"],
        ["video"] = [with(StringComparer.OrdinalIgnoreCase), "src", "poster", "controls", "autoplay", "loop", "muted", "preload", "width", "height"],
        ["audio"] = [with(StringComparer.OrdinalIgnoreCase), "src", "controls", "autoplay", "loop", "muted", "preload"],
        ["source"] = [with(StringComparer.OrdinalIgnoreCase), "src", "type"],
        ["th"] = [with(StringComparer.OrdinalIgnoreCase), "colspan", "rowspan", "scope", "headers", "align"],
        ["td"] = [with(StringComparer.OrdinalIgnoreCase), "colspan", "rowspan", "headers", "align"],
        ["table"] = [with(StringComparer.OrdinalIgnoreCase), "border", "cellpadding", "cellspacing"],
        ["ol"] = [with(StringComparer.OrdinalIgnoreCase), "start", "type", "reversed"],
        ["li"] = [with(StringComparer.OrdinalIgnoreCase), "value"]
    };

    private static readonly HashSet<string> StyleProperties =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "color", "background-color", "font-family", "font-size", "font-weight", "font-style",
        "text-align", "text-decoration", "line-height",
        "margin", "margin-block", "margin-block-start", "margin-block-end",
        "margin-inline", "margin-inline-start", "margin-inline-end",
        "padding", "padding-block", "padding-block-start", "padding-block-end",
        "padding-inline", "padding-inline-start", "padding-inline-end",
        "border", "border-color", "border-width", "border-style",
        "width", "height", "max-width", "max-height", "min-width", "min-height"
    ];

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

    // Each lookup hands back the allowlist's own spelling as the canonical name.
    private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> TagLookup = Tags.GetAlternateLookup<ReadOnlySpan<char>>();
    private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> DiscardedLookup = DiscardedWithContent.GetAlternateLookup<ReadOnlySpan<char>>();
    private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> GlobalAttributeLookup = GlobalAttributes.GetAlternateLookup<ReadOnlySpan<char>>();
    private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> StylePropertyLookup = StyleProperties.GetAlternateLookup<ReadOnlySpan<char>>();
    private static readonly Dictionary<string, HashSet<string>>.AlternateLookup<ReadOnlySpan<char>> TagAttributeLookup =
        TagAttributes.GetAlternateLookup<ReadOnlySpan<char>>();

    /// <inheritdoc />
    public bool Allows(HtmlSubject subject, ReadOnlySpan<char> value, ReadOnlySpan<char> owner, out string? canonicalSpelling)
    {
        canonicalSpelling = null;

        return subject switch
        {
            HtmlSubject.Element => TagLookup.TryGetValue(value, out canonicalSpelling),
            HtmlSubject.ElementContent => !DiscardedLookup.Contains(value),
            HtmlSubject.Attribute => AllowsAttribute(value, owner, out canonicalSpelling),
            HtmlSubject.StyleProperty => AllowsStyleProperty(value, out canonicalSpelling),
            HtmlSubject.StyleDeclaration => !IsDenied(value),
            HtmlSubject.Url => IsAllowedUrl(value),
            _ => false,
        };
    }

    private static bool AllowsAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> owner, out string? canonicalSpelling)
    {
        if (GlobalAttributeLookup.TryGetValue(name, out canonicalSpelling))
        {
            return true;
        }

        if (TagAttributeLookup.TryGetValue(owner, out var set)
            && set.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(name, out canonicalSpelling))
        {
            return true;
        }

        // Any data attribute is permitted in the spelling it was written with.
        canonicalSpelling = null;

        return name.StartsWith("data-", StringComparison.OrdinalIgnoreCase);
    }

    private static bool AllowsStyleProperty(ReadOnlySpan<char> property, out string? canonicalSpelling)
    {
        if (StylePropertyLookup.TryGetValue(property, out canonicalSpelling))
        {
            return true;
        }

        // Any Forma custom property is permitted in the spelling it was written with.
        canonicalSpelling = null;

        return property.StartsWith("--forma-", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDenied(ReadOnlySpan<char> declaration)
    {
        foreach (var deniedStyle in StyleDenyList)
        {
            if (declaration.Contains(deniedStyle, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAllowedUrl(ReadOnlySpan<char> url)
    {
        var trimmedUrl = url.Trim();

        if (trimmedUrl.IsEmpty)
        {
            return false;
        }

        if (trimmedUrl[0] == '#')
        {
            return trimmedUrl.Length > 1;
        }

        // A relative path takes the document's own scheme.
        if (trimmedUrl[0] == '/'
            || trimmedUrl.StartsWith("./", StringComparison.Ordinal)
            || trimmedUrl.StartsWith("../", StringComparison.Ordinal)
            || IsRelativePath(trimmedUrl))
        {
            return true;
        }

        foreach (var scheme in UrlSchemes)
        {
            if (trimmedUrl.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
            {
                return trimmedUrl.Length > scheme.Length;
            }
        }

        foreach (var prefix in DataUrlPrefixes)
        {
            if (trimmedUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                // A data URL's bytes follow its comma.
                return trimmedUrl.IndexOf(',') > 0;
            }
        }

        return false;
    }

    // A colon before the first separator would name a scheme, and no address holds whitespace or a control character.
    private static bool IsRelativePath(ReadOnlySpan<char> url)
    {
        var separator = url.IndexOfAny('/', '?', '#');
        var firstSegment = separator < 0 ? url : url[..separator];

        return !firstSegment.Contains(':') && url.IndexOfAnyInRange('\0', ' ') < 0 && !url.Contains('\u007f');
    }
}
