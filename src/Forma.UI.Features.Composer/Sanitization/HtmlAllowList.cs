namespace Forma.UI.Features.Composer.Sanitization;

using System.Buffers;

/// <summary>
/// The markup <see cref="DefaultHtmlSanitizer"/> permits: the elements, attributes, inline style properties
/// and URL schemes a document may carry, alongside the alternate lookups the sanitizer matches a sliced tag
/// or attribute name against without first copying it to a string.
/// </summary>
internal static class HtmlAllowList
{
    /// <summary>Elements a document may carry.</summary>
    public static readonly HashSet<string> Tags = new(StringComparer.OrdinalIgnoreCase)
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

    /// <summary>Elements that carry no closing tag.</summary>
    public static readonly HashSet<string> VoidTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "img", "hr", "br", "source", "col", "wbr"
    };

    /// <summary>Elements dropped together with their content, whose text would otherwise survive as markup or script.</summary>
    public static readonly HashSet<string> DiscardedWithContent = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "iframe", "object", "svg", "math", "noscript", "template", "applet", "frameset"
    };

    /// <summary>Attributes permitted on any allowed element.</summary>
    public static readonly HashSet<string> GlobalAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "class", "id", "style", "dir", "title", "lang"
    };

    /// <summary>Attributes permitted only on the element that names them.</summary>
    public static readonly Dictionary<string, HashSet<string>> TagAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a"] = new(StringComparer.OrdinalIgnoreCase) { "href", "target", "rel", "download" },
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

    /// <summary>CSS properties an inline style may set.</summary>
    public static readonly HashSet<string> StyleProperties = new(StringComparer.OrdinalIgnoreCase)
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

    /// <summary>Attribute names carrying a URL, which is checked before the attribute is kept.</summary>
    public static readonly HashSet<string> UrlAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "href", "src", "poster"
    };

    /// <summary>URL schemes a document may link to or embed.</summary>
    public static readonly string[] UrlSchemes =
    [
        "http://", "https://", "mailto:", "tel:"
    ];

    /// <summary>Inline image types a data URL may carry; SVG is excluded because it can carry script.</summary>
    public static readonly string[] DataUrlPrefixes =
    [
        "data:image/png", "data:image/jpeg", "data:image/jpg",
        "data:image/gif", "data:image/webp", "data:image/avif"
    ];

    /// <summary>CSS constructs that disqualify a declaration however it is spelled.</summary>
    public static readonly string[] StyleDenyList =
    [
        "url(", "javascript:", "expression(", "@import", "behavior:"
    ];

    /// <summary>The characters an attribute value may not carry unescaped.</summary>
    public static readonly SearchValues<char> EscapeTargets = SearchValues.Create("&\"<>");

    // The alternate lookups match a sliced name without copying it to a string first, and hand back the
    // allowlist's own spelling, so the output is normalized without a ToLowerInvariant per tag.

    /// <summary>Matches an element name against <see cref="Tags"/> without allocating.</summary>
    public static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> TagLookup = Tags.GetAlternateLookup<ReadOnlySpan<char>>();

    /// <summary>Matches an element name against <see cref="VoidTags"/> without allocating.</summary>
    public static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> VoidTagLookup = VoidTags.GetAlternateLookup<ReadOnlySpan<char>>();

    /// <summary>Matches an element name against <see cref="DiscardedWithContent"/> without allocating.</summary>
    public static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> DiscardedLookup = DiscardedWithContent.GetAlternateLookup<ReadOnlySpan<char>>();

    /// <summary>Matches an attribute name against <see cref="GlobalAttributes"/> without allocating.</summary>
    public static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> GlobalAttributeLookup = GlobalAttributes.GetAlternateLookup<ReadOnlySpan<char>>();

    /// <summary>Matches a CSS property against <see cref="StyleProperties"/> without allocating.</summary>
    public static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> StylePropertyLookup = StyleProperties.GetAlternateLookup<ReadOnlySpan<char>>();

    /// <summary>Matches an attribute name against <see cref="UrlAttributes"/> without allocating.</summary>
    public static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> UrlAttributeLookup = UrlAttributes.GetAlternateLookup<ReadOnlySpan<char>>();

    /// <summary>Looks up an element's own attribute set without allocating.</summary>
    public static readonly Dictionary<string, HashSet<string>>.AlternateLookup<ReadOnlySpan<char>> TagAttributeLookup =
        TagAttributes.GetAlternateLookup<ReadOnlySpan<char>>();
}
