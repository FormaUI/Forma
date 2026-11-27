# Forma.Framework.Themes

The contracts of Forma's theme system. A theme is a named palette, the rule `[data-forma-theme="its-name"]`, drawn in a
light and a dark color scheme, as the [theme design](https://github.com/FormaUI/Forma/blob/main/docs/theme-design.md)
sets out.

## Install

```pwsh
dotnet add package formaui-net.Framework.Themes --prerelease
```

## Registering themes

Forma's component library registers its own two themes, Forma and Forma High Contrast, and a reader starts in Forma's
theme in the System preference. A host adds its themes, removes or replaces Forma's, or sets another starting choice,
in any order beside the component library's registration:

```csharp
builder.Services.AddFormaThemes(themes =>
{
    themes.Add(new ThemeDefinition("harbor", LocalizableText.For<Program>("Harbor"), ColorSchemes.Light | ColorSchemes.Dark, "css/harbor-theme.css"));
    themes.Default = new ThemeChoice("harbor", ColorSchemePreference.System);
});
```

A default naming a theme that isn't registered throws `InvalidOperationException` as the registry is made, and a second
theme of a registered name as it's added.

## The types

| Type | What it is |
| --- | --- |
| `ThemeDefinition` | A theme a host offers: its name, the name a reader sees, the schemes it draws, its light one among them, and its stylesheet. |
| `ThemeOptions` | What `AddFormaThemes` configures: the themes registered, in the order a reader is offered them, and the `Default` choice. |
| `ThemeRegistry` | The themes registered and the default, resolving a choice naming a theme that isn't registered to the default theme in the choice's scheme preference, and a theme with a light scheme alone to its light scheme. |
| `ThemeChoice` | What a reader chose: a theme's name and a `ColorSchemePreference`. |
| `ThemePreference` | A choice as a host's store keeps it, with the revision Forma numbered it by. |
| `IThemeState` | The choice a user scope draws, announced to its subscribers as it changes. |
| `IThemeSwitch` | Requests a choice for every runtime on the page. |
| `IThemePreferenceStore` | The store a host implements to keep a signed-in reader's preference on every device. |
| `ColorSchemes` | The color schemes a theme is drawn in, `Light` and `Dark`, combined as flags. |
| `ColorSchemePreference` | The color scheme a reader prefers a theme drawn in: `System`, the device's, `Light` or `Dark`. |

```csharp
using Forma.Framework.Themes;

var schemes = ColorSchemes.Light | ColorSchemes.Dark;
var preference = ColorSchemePreference.System;
```

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
