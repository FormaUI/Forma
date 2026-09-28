# Forma.Framework.Css

Value types and builders that render a CSS `class` or `style` attribute value from C#. A length
or a color is a typed value that prints itself as valid CSS, culture-invariant, and a builder
collects what an element carries and renders it once as one attribute.

## Install

```pwsh
dotnet add package FormaUI.NET-Framework.Css
```

The package has no dependencies and registers nothing; every type is a plain value or builder.

## The types

| Type | What it is |
| --- | --- |
| `CssClassBuilder` | Collects class names and renders them space-separated. |
| `CssStyleBuilder` | Collects declarations and renders them as `property: value;` pairs. |
| `CssLength` | An immutable measurement with a unit, rendered as `20rem`, `6ch`, `50%`. |
| `CssLengthUnit` | The units a `CssLength` is measured in: `Ch`, `Em`, `Percent`, `Px`, `Rem`. |
| `CssColor` | An immutable RGBA color, rendered as `#rrggbb` or `rgba(r, g, b, a)`. |

## Classes

`Add` takes a class name, or a name and the condition under which it applies, and ignores a blank
name. `ToString` renders what was added, and `ToStringOrNull` answers `null` when nothing was, so a
Blazor `class` attribute is omitted rather than rendered empty.

```csharp
using Forma.Framework.Css;

var classes = new CssClassBuilder()
    .Add("forma-button")
    .Add("forma-button--primary", isPrimary)
    .Add("forma-button--busy", isBusy);

// <button class="@classes.ToStringOrNull()">
```

`IsEmpty` reports whether anything was added, and `Clear` starts the builder over.

## Styles

`Add` takes a property and a string value, a property and a `CssLength`, or CSS text already written
as declarations; a blank value or a `null` length adds nothing, and a conditional overload adds the
declaration only while its condition holds. Every declaration is terminated, whatever form it was
given in.

```csharp
using Forma.Framework.Css;

CssLength? height = null;
var isFloating = true;

var style = new CssStyleBuilder()
    .Add("inline-size", CssLength.FromRem(18.5))
    .Add("block-size", height)
    .Add("left", "40px", isFloating)
    .Add("--forma-accent-color: red");

style.ToString();        // "inline-size: 18.5rem; left: 40px; --forma-accent-color: red;"
style.ToStringOrNull();  // the same, or null when nothing was added
```

## Lengths

A length is created through a factory named for its unit and prints itself with up to four decimal
places, invariant of the current culture.

```csharp
CssLength.FromCh(6).ToString();        // "6ch"
CssLength.FromRem(1.25).ToString();    // "1.25rem"
CssLength.FromPercent(50).ToString();  // "50%"
CssLength.FromPx(12).Unit;             // CssLengthUnit.Px
```

A non-finite value throws `ArgumentOutOfRangeException`.

## Colors

A color is created from a packed 24-bit value or from its channels, with an optional opacity from
0 to 1. `ToString` renders the shortest CSS value that preserves the color: `#rrggbb` when it is
opaque, `rgba(r, g, b, a)` otherwise. `ToHexString` and `ToRgbaString` render one form regardless.

```csharp
CssColor.FromHex(0x2c384a).ToString();              // "#2c384a"
CssColor.FromRgba(98, 105, 118, 0.12).ToString();   // "rgba(98, 105, 118, 0.12)"
CssColor.FromRgb(255, 193, 7).IsOpaque;             // true
```

A packed value outside `0x000000`–`0xFFFFFF` or an opacity outside 0–1 throws
`ArgumentOutOfRangeException`.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
