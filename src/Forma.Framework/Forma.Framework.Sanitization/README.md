# Forma.Framework.Sanitization

An HTML sanitizer for rich content a reader authored, with no external dependency. The sanitizer
keeps what a markup policy allows and removes everything else, so a document from an editor or a
paste can be rendered as raw markup.

## Install and register

```pwsh
dotnet add package FormaUI.NET-Framework.Sanitization
```

```csharp
using Forma.Framework.Sanitization;

builder.Services.AddFormaSanitizationServices();
```

That registers `IMarkupPolicy` as `RichContentMarkupPolicy.Default` and `IHtmlSanitizer` as
`AllowlistHtmlSanitizer`, both singletons. Registration uses `TryAdd`, and the two register
separately, so a host narrowing what its documents may carry registers its own `IMarkupPolicy`
first and keeps the sanitizer.

## The contracts

| Member | What it is |
| --- | --- |
| `IHtmlSanitizer.Sanitize(html)` | Returns the content with disallowed markup removed; blank input returns an empty string. |
| `IHtmlSanitizer.MarkupPolicy` | The policy a document sanitized by this instance is judged by. |
| `IMarkupPolicy.Allows(subject, value, owner, out canonical)` | Whether a subject is permitted, and the spelling to write it with, or `null` to keep the one given. |
| `HtmlSubject` | What a policy is asked about: `Element`, `ElementContent`, `Attribute`, `StyleProperty`, `StyleDeclaration`, `Url`. |

`AllowlistHtmlSanitizer` implements `IHtmlSanitizer` over any policy; its parameterless constructor
applies `RichContentMarkupPolicy.Default`, and `AllowlistHtmlSanitizer(IMarkupPolicy)` applies the
one given.

## Sanitizing a document

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

## What every policy gets

Whatever the policy says, the sanitizer removes an attribute whose name starts with `on`, drops
comments, doctypes and processing instructions, removes an element the policy refuses the content
of together with everything inside it, leaves a lone `<` that opens no tag as text, writes a void
element such as `br` or `img` self-closed and drops its closing tag, and re-escapes every kept
attribute value rather than passing it through. A `style` attribute is kept only while at least one
of its declarations is allowed.

## The default policy

`RichContentMarkupPolicy` allows the markup a rich text editor produces and nothing else:

- **Elements** — text structure and inline formatting, lists, links, images, video and audio,
  tables, and the grouping elements a document is laid out with. An element that scripts, styles
  or embeds — `script`, `style`, `iframe`, `object`, `svg` among them — is removed with its content.
- **Attributes** — on those elements, the attributes that describe content rather than behavior:
  identity and language on any element, the address and target of a link, the source and
  dimensions of media, the shape of a table cell, and any `data-` attribute.
- **Inline styles** — color, typography, alignment, spacing, borders and size, plus any custom
  property named `--forma-`. A declaration that could load or run anything — `url(`,
  `javascript:`, `expression(`, `@import`, `behavior:` — is dropped.
- **URLs** — `http`, `https`, `mailto` and `tel`, a path relative to the document, a fragment, and
  a `data:` URL carrying a raster image. Everything else, an SVG data URL included, is refused.

`RichContentMarkupPolicy` itself is the authority on each list. A kept element, attribute and
style property is written in the policy's lowercase spelling; a `data-` attribute and a
`--forma-` property keep the spelling they were written with.

## Writing a policy

A host whose documents may carry less, or different, markup implements `IMarkupPolicy` and answers
per subject. This policy keeps the default rules for everything but elements, and permits only the
inline ones:

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
