# Forma.Framework.Themes

The contracts of Forma's theme system. A theme is a named palette, the rule `[data-forma-theme="its-name"]`, drawn in a
light and a dark color scheme, as the [theme design](https://github.com/FormaUI/Forma/blob/main/docs/theme-design.md)
sets out.

## Install

```pwsh
dotnet add package formaui-net.Framework.Themes --prerelease
```

Every type is used directly; there is nothing to register.

| Type | What it is |
| --- | --- |
| `ColorSchemes` | The color schemes a theme is drawn in, `Light` and `Dark`, combined as flags. |
| `ColorSchemePreference` | The color scheme a reader prefers a theme drawn in: `System`, the device's, `Light` or `Dark`. |

```csharp
using Forma.Framework.Themes;

var schemes = ColorSchemes.Light | ColorSchemes.Dark;
var preference = ColorSchemePreference.System;
```

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
