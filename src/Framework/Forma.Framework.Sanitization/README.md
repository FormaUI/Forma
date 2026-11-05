# Forma.Framework.Sanitization

An allowlist HTML sanitizer for content people author, such as a rich text editor's output or a
pasted document, so the application can render it as markup. It keeps what a markup policy
permits and removes the rest, and it depends on nothing beyond .NET itself.

## Install and register

```pwsh
dotnet add package formaui-net.Framework.Sanitization --prerelease
```

```csharp
using Forma.Framework.Sanitization;

builder.Services.AddFormaSanitizationServices();
```

`AddFormaSanitizationServices` registers `IMarkupPolicy` as `RichContentMarkupPolicy.Default` and
`IHtmlSanitizer` as `AllowlistHtmlSanitizer`, both singletons, each with `TryAdd`, so a policy the
host registered first is the one the sanitizer applies.

| Member | What it is |
| --- | --- |
| `IHtmlSanitizer.Sanitize(html)` | The HTML with everything its policy refuses removed; an empty string for blank input. |
| `IHtmlSanitizer.MarkupPolicy` | The policy the sanitizer applies. |
| `IMarkupPolicy.Allows(subject, value, owner, out canonical)` | Whether a subject is permitted, and the spelling to write it in, or `null` to keep the one given. |
| `HtmlSubject` | What a policy is asked about: `Element`, `ElementContent`, `Attribute`, `StyleProperty`, `StyleDeclaration` or `Url`. |

`AllowlistHtmlSanitizer` can also be created directly: with no argument it applies
`RichContentMarkupPolicy.Default`, and with an `IMarkupPolicy` it applies that one.

## Sanitizing

```csharp
using Forma.Framework.Sanitization;

public sealed class ArticleService(IHtmlSanitizer sanitizer)
{
    public string Prepare(string authoredHtml)
        => sanitizer.Sanitize(authoredHtml);
}
```

```csharp
var sanitizer = new AllowlistHtmlSanitizer();

sanitizer.Sanitize("<p onclick=\"steal()\">Safe <b>text</b></p><script>alert(1)</script>");
// "<p>Safe <b>text</b></p>"

sanitizer.Sanitize("<a href=\"javascript:alert(1)\" title=\"a > b\">Click</a>");
// "<a title=\"a &gt; b\">Click</a>"

sanitizer.Sanitize("<P CLASS=\"lead\">Text<br></br></P>");
// "<p class=\"lead\">Text<br /></p>"
```

Under any policy, the sanitizer drops event handler attributes, those whose name starts with
`on`, along with comments, doctypes and processing instructions; removes an element whose content
the policy refuses together with that content; writes void elements such as `br` and `img`
self-closed; and re-escapes every attribute value it keeps. A `style` attribute is kept while at
least one of its declarations is permitted.

## The default policy

`RichContentMarkupPolicy` permits the markup a rich text editor produces:

- **Elements**: headings, paragraphs, quotes and code; inline formatting; line breaks and rules;
  lists; links; images, video and audio; tables; `span`, `div`, `details`, `summary`, `figure` and
  `figcaption`.
  `script`, `style`, `iframe`, `object`, `svg`, `math`, `noscript`, `template`, `applet` and
  `frameset` are removed with their content.
- **Attributes**: `class`, `id`, `style`, `dir`, `title` and `lang` on any element; the address,
  target and relation of a link; the source and dimensions of media; the span and scope of a table
  cell; a list's numbering; and any `data-` attribute.
- **Inline styles**: color, font, text alignment and decoration, line height, margin, padding,
  border and size, and any custom property starting with `--forma-`. A declaration containing
  `url(`, `javascript:`, `expression(`, `@import` or `behavior:` is dropped.
- **URLs**: `http`, `https`, `mailto` and `tel`; a path within the site, rooted at `/` or relative to the
  document, whose first segment holds no colon and which holds no whitespace; a
  fragment; and a `data:` URL carrying a PNG, JPEG, GIF, WebP or AVIF image.

Elements, attributes and style properties are written in lowercase; a `data-` attribute and a
`--forma-` property keep the spelling they were written in.

## Writing a policy

A host whose documents carry different markup implements `IMarkupPolicy`. This one permits only
inline elements and defers everything else to the default policy:

```csharp
using Forma.Framework.Sanitization;

public sealed class CommentMarkupPolicy : IMarkupPolicy
{
    private static readonly HashSet<string> InlineTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "b", "strong", "i", "em", "code", "a", "br"
    };

    public bool Allows(HtmlSubject subject, ReadOnlySpan<char> value, ReadOnlySpan<char> owner, out string? canonical)
    {
        if (subject == HtmlSubject.Element && !InlineTags.Contains(value.ToString()))
        {
            canonical = null;
            return false;
        }

        return RichContentMarkupPolicy.Default.Allows(subject, value, owner, out canonical);
    }
}
```

```csharp
builder.Services.AddSingleton<IMarkupPolicy, CommentMarkupPolicy>();
builder.Services.AddFormaSanitizationServices();
```

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
