# Forma.Blazor.Locales.Hebrew

The Hebrew localization of Forma, as a locale pack: a package of locale files alone, carrying the
Hebrew strings of `Forma.Blazor`, `Forma.Blazor.Composer`, `Forma.Blazor.DataView` and
`Forma.Blazor.Explorer`. Installing it is all an application does to show Forma's components in
Hebrew.

## Install

```pwsh
dotnet add package formaui-net.Blazor.Locales.Hebrew
dotnet add package formaui-net.Framework.Localization.Json
```

Reference the pack from the application itself. The package records the pack in the application
when it builds, and `AddJsonLocalization()` reads it:

```csharp
using Forma.Blazor;
using Forma.Framework.Localization.Json;

builder.Services.AddJsonLocalization();
builder.AddFormaUIServer();
```

Forma's strings then appear in Hebrew wherever the culture state's UI culture is `he` or a culture
beneath it, such as `he-IL`. The pack carries strings for each of the four assemblies, so it serves
whichever of them the application installs.

## Rewording a string

An application's own `.locales` folder overrides the pack. A file there, in a folder named for the
assembly and laid out by the type's namespace, holds only the keys it changes:

```text
.locales/Forma.Blazor/Components/Bars/he.json
```

```json
{
  "$localization": "SchemaVersion: 1.0.0; Culture: he;",
  "FormaSelectionToolbar": {
    "Clear Selection": "בטל בחירה"
  }
}
```

[`Forma.Framework.Localization`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Localization/README.md)
documents how a host lays out its files and copies them to the output, and how a class library or
a test names a pack in `LocalizationOptions.LocalePacks`.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
