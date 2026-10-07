# Forma.Framework.Css

Typed CSS values and attribute builders for Blazor markup. `CssLength` and `CssColor` render
themselves as valid, culture-invariant CSS, and `CssClassBuilder` and `CssStyleBuilder` collect
what an element carries and render it as one `class` or `style` attribute value.

## Install

```pwsh
dotnet add package formaui-net.Framework.Css
```

Every type is created directly; there is nothing to register.

| Type | What it is |
| --- | --- |
| `CssClassBuilder` | Collects class names and renders them space-separated. |
| `CssStyleBuilder` | Collects declarations and renders them as `property: value;` pairs. |
| `CssLength` | An immutable measurement with a unit, rendered as `20rem`, `6ch` or `50%`. |
| `CssLengthUnit` | The unit of a `CssLength`: `Ch`, `Em`, `Percent`, `Px` or `Rem`. |
| `CssColor` | An immutable RGBA color, rendered as `#rrggbb` or `rgba(r, g, b, a)`. |

## Classes

`Add` takes a class name, optionally with the condition under which it applies, and skips a blank
name. `ToStringOrNull` returns `null` when nothing was added, so Blazor omits the attribute.

```razor
@using Forma.Framework.Css

<button class="@Classes.ToStringOrNull()">Save</button>

@code {
    [Parameter] public bool IsPrimary { get; set; }
    [Parameter] public bool IsBusy { get; set; }

    private CssClassBuilder Classes => new CssClassBuilder()
        .Add("app-button")
        .Add("app-button--primary", IsPrimary)
        .Add("app-button--busy", IsBusy);
}
```

`IsEmpty` says whether anything was added, and `Clear` empties the builder.

## Styles

`Add` takes a property with a string value, a property with a `CssLength`, or declarations already
written as CSS text, and a conditional overload adds a declaration only while its condition holds.
A blank value or a `null` length adds nothing, and every declaration is terminated with `;`.

```csharp
using Forma.Framework.Css;

CssLength? height = null;
var isFloating = true;

var style = new CssStyleBuilder()
    .Add("inline-size", CssLength.FromRem(18.5))
    .Add("block-size", height)
    .Add("inset-inline-start", "40px", isFloating)
    .Add("--app-accent: rebeccapurple");

style.ToString();  // "inline-size: 18.5rem; inset-inline-start: 40px; --app-accent: rebeccapurple;"
```

## Lengths

A length is created through the factory for its unit and renders with up to four decimal places.
A non-finite value throws `ArgumentOutOfRangeException`.

```csharp
CssLength.FromCh(6).ToString();        // "6ch"
CssLength.FromRem(1.25).ToString();    // "1.25rem"
CssLength.FromPercent(50).ToString();  // "50%"
CssLength.FromPx(12).Unit;             // CssLengthUnit.Px
```

## Colors

A color is created from a packed 24-bit value or from its channels, with an optional opacity from
0 to 1. `ToString` renders `#rrggbb` for an opaque color and `rgba(r, g, b, a)` otherwise;
`ToHexString` and `ToRgbaString` render one form always. A packed value outside `0x000000` to
`0xFFFFFF` or an opacity outside 0 to 1 throws `ArgumentOutOfRangeException`.

```csharp
CssColor.FromHex(0x2c384a).ToString();             // "#2c384a"
CssColor.FromRgba(98, 105, 118, 0.12).ToString();  // "rgba(98, 105, 118, 0.12)"
CssColor.FromRgb(255, 193, 7).IsOpaque;            // true
```

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
