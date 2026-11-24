# Forma.Framework.Sanitization

An allowlist HTML sanitizer for content people author, such as a rich text editor's output or a
pasted document, so the application can render it as markup. It keeps what a markup policy
permits and removes the rest, and it depends on nothing beyond .NET itself. Beside it stands
`AddressPolicy`, the rule for the addresses written into a page, which Forma's components and the
default policy apply.

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
| `IMarkupPolicy.Allows(subject, value, owner, out canonical)` | Whether a subject is permitted, and the spelling to write it in, or `null` to keep the one given. The owner is the element an attribute or an address belongs to. |
| `HtmlSubject` | What a policy is asked about: `Element`, `ElementContent`, `Attribute`, `StyleProperty`, `StyleDeclaration` or `Url`. |
| `AddressPolicy.Allows(address, kind)` | Whether an address of an `AddressKind` may be the given address. |
| `AddressPolicy.GetScheme(address)` | The address's scheme as the browser reads it, in lowercase, or `null` for a relative address. |

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
- **URLs**: what the address rule below allows for the element the address belongs to, judged as
  a link on an `a`, as an image's source on an `img`, as a video's source or its poster on a
  `video`, as an audio's source on an `audio`, and as a video's or an audio's on a `source`. The
  address also has to name something: a path within the site, rooted at `/` or relative to the
  document, whose first segment holds no colon and which holds no whitespace; a fragment past its
  `#`; or something past a scheme's colon and the slashes before its host.

Elements, attributes and style properties are written in lowercase; a `data-` attribute and a
`--forma-` property keep the spelling they were written in.

## Addresses

`AddressPolicy` decides which addresses may be written where, so that none runs script where it
stands. What an address is to its element is its `AddressKind`, and each kind allows the schemes
in its row. A relative address takes the page's own scheme, and every kind allows it.

| `AddressKind` | Written as | Allows |
| --- | --- | --- |
| `Link` | A link a reader follows, an anchor's `href` | `http`, `https`, `mailto`, `tel` |
| `DownloadLink` | A link that downloads, an anchor carrying `download` | A link's schemes, `blob:` and any `data:` |
| `ImageSource` | An image's `src`, a video's `poster` | `http`, `https`, `blob:`, and `data:` of a PNG, APNG, JPEG, GIF, WebP, AVIF, BMP or icon image |
| `VideoSource` | A video's `src` | `http`, `https`, `blob:`, and `data:` of a video |
| `AudioSource` | An audio's `src` | `http`, `https`, `blob:`, and `data:` of an audio |
| `TrackSource` | A text track's `src`, such as a video's captions | `http`, `https`, `blob:`, and `data:` of WebVTT |
| `FrameSource` | A frame's `src`, an object's `data` | `http`, `https`, `blob:` |

```csharp
using Forma.Framework.Sanitization;

AddressPolicy.Allows("https://example.com/stage-12", AddressKind.Link);        // true
AddressPolicy.Allows("JaVaScRiPt:alert(1)", AddressKind.Link);                 // false
AddressPolicy.Allows("data:image/png;base64,iVBORw0KGgo=", AddressKind.ImageSource); // true
AddressPolicy.Allows("data:image/png;base64,iVBORw0KGgo=", AddressKind.Link);  // false
AddressPolicy.GetScheme("\u0001 java\tscript:alert(1)");                       // "javascript"
```

An address is judged as the browser reads it. The browser drops the control characters and spaces
at either end of an address and every tab and line break within it, so `java&#9;script:` and a
`javascript:` behind a control character are refused as `javascript:` is. Its scheme is what stands
before the first colon where that is a letter followed by letters, digits, `+`, `-` or `.`, in any
case. A `data:` address is judged by the media type before its comma, and one without a comma
carries nothing. An SVG image is left out of the image kind, since an SVG document can carry
script.

Forma's components write an address its kind doesn't allow inert: the element keeps its text and
takes no address, and the refusal is logged at `Debug` under the component, naming the scheme
rather than the address.

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
