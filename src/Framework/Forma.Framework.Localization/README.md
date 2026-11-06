# Forma.Framework.Localization

A localization contract for .NET libraries and applications. A type looks a string up by its
English source text, so a string no locale file localizes renders as written, and localized strings
are found by the name of the type asking. The package carries the
hole and plural syntax a string is written in, the CLDR plural rules, and the build that makes a
project a locale pack.

## Install and register

A library that looks strings up references the contract alone:

```pwsh
dotnet add package formaui-net.Framework.Localization --prerelease
```

An application registers a backend to read the locale files. `Forma.Framework.Localization.Json`
is the one Forma ships, reading JSON locale files:

```pwsh
dotnet add package formaui-net.Framework.Localization.Json --prerelease
```

```csharp
using Forma.Framework.Localization.Json;

builder.Services.AddJsonLocalization();
```

That registers `IStringLocaleFactory` and `IStringLocale<T>`. A backend of your own implements the
same two contracts. `NullStringLocaleFactory.Instance` is a factory whose locales return every key
as its own text, with holes filled and plural forms resolved in the source language, and
`GetStringLocaleFactory` resolves the registered factory or that one, which is how Forma's components
read their strings where a host registers no localization.

## Looking a string up

Inject the locale for the type and look each string up by its source text:

```csharp
using Forma.Framework.Localization;

public sealed class UploadStatus(IStringLocale<UploadStatus> locale)
{
    public string Saved
        => locale["Saved"];

    public string Progress(int stored, int total)
        => locale["Stored {Stored} of {Total}.", stored, total];

    public string FileCount(int count)
        => locale["[$ plural; one: # file; other: # files; $]", count];
}
```

A Razor component injects the same locale, `@inject IStringLocale<UploadPage> L`. A locale for a
type chosen at runtime comes from the factory, `factory.Create(typeof(UploadStatus))`.

A lookup returns a `LocalizedText`, which converts to `string` and reports how the lookup went:

```csharp
var text = locale["Save"];

text.Value              // "Guardar" under es; "Save" where no locale file localizes it
text.ResourceNotFound   // true when the lookup fell back to the key
text.SearchedLocation   // the full name of the type that was searched
```

`GetAllStrings(includeParentCultures)` lists every localized string the locale's UI culture has for the type.

### Cultures

A locale chooses its strings, their fallback and their plural forms by `CultureInfo.CurrentUICulture`,
and formats the values in their holes by `CultureInfo.CurrentCulture`. `WithCulture(culture, uiCulture)`
returns a locale that uses the two cultures given instead, reading the same files; the original keeps
reading the current ones:

```csharp
var hebrew = locale.WithCulture(CultureInfo.GetCultureInfo("he"), CultureInfo.GetCultureInfo("he"));

hebrew["Save"]   // "שמירה", whatever the current culture
```

`factory.Create(type, cultureState)` creates a locale that looks its strings up in an `ICultureState`'s
cultures, bound again on its first use after each change of the state, which is how a component on
Forma's bases reads its strings. A component of a host that switches language in place reads its
strings this way too, since a render can run in a flow that still holds an earlier ambient culture,
such as one that began before the switch.

### Holes

A hole is `{Name}`, optionally with an alignment and a format specifier, `{Name,8}` or
`{Ratio:P1}`. Values pair with the key's holes by position, in the order the holes first appear,
one value per distinct hole; any other count throws `ArgumentException`. A localized string refers to
the holes by name, so it may reorder them, and it may choose its own specifier. A literal brace is
doubled, `{{`, in keys and localized strings alike.

### Plural forms

A plural expression selects one of its fields by a count:

```text
[$ [Files] plural; one: # file; other: # files; $]
```

`[Files]` names the argument holding the count, and is left out when the count is the only value.
`#` in a field is the count, formatted for the culture on screen. The categories are CLDR's,
`zero`, `one`, `two`, `few`, `many` and `other`, and each language uses the ones it distinguishes:
English `one` and `other`, Hebrew adds `two`. A field a localized string omits falls back to `other`.
A key no locale file localizes selects its field by the rules of `SourceLanguage`.

The rules are CLDR 48's cardinal rules for every locale CLDR publishes, held by
`CommonPluralRuleSet.Default` in `Forma.Framework.Localization.Language`, which also answers a
count's category directly:

```csharp
CommonPluralRuleSet.Default.Select(CultureInfo.GetCultureInfo("he"), 2)   // PluralCategory.Two
```

### Rendering holes as markup

`Resolve` returns the localized format with its plural forms selected and its holes still open, so
a caller can render a hole as something other than text, such as a link or a component:

```csharp
var pattern = locale.Resolve("Capped at {Limit} here.", limit);

foreach (var segment in pattern.EnumerateSegments())
{
    if (segment.IsHole)
    {
        // render the argument in pattern.Arguments whose Name matches segment.Name
    }
    else
    {
        // render segment.Text
    }
}
```

Segments come in the order the localized string writes them.

## Where locale files live

**In the assembly.** A folder keeps its locale files in a `.locales` folder of its own, one file
per culture. A file named for the culture alone holds a section per type in the folder; a file
named for a type holds that type's entries. Both of these localize the
`UploadStatus` above, in a folder `Uploads`:

```text
Uploads/.locales/he.json                   { "UploadStatus": { "Saved": "נשמר" } }
Uploads/.locales/UploadStatus.he.json      { "Saved": "נשמר" }
```

A folder's file may also sit higher up, spelling the rest of the name in its sections. A regional
culture's file, `es-419.json`, holds only what differs from its parent's.

**Embedded by the build.** The build embeds every locale file it finds by the `$localization` entry
each one carries, wherever it sits in the project, outside the build output and outside a folder
whose name starts with a dot but `.locales`, so a project names none of them; a file the project
embeds through an item of its own keeps the name that item gives it, and one an item in the project
file copies beside its output stays a file there.

**In the host.** An application rewords a string, or adds a language the library never shipped, in
its root `.locales` folder, beside its own root files: a folder named for the assembly, then the
type's namespace as folders, holding only the keys it changes:

```text
.locales/Forma.Blazor/Components/Bars/he.json             { "FormaSelectionToolbar": { "Clear Selection": "בטל בחירה" } }
.locales/Forma.Blazor/Components/Bars/fr.json             { "FormaSelectionToolbar": { "Clear Selection": "Effacer la sélection" } }
```

An application, a WebAssembly client, a server built on the Web SDK or a Blazor Hybrid application,
embeds these in its assembly, under resource names starting with `.locales.`, so a browser and an
application MAUI packages, with no folder to read, read them too. A Web App's server reads its
client's as well: its build records each client it references whose build embedded such files with
`LocaleReferenceAttribute`, so a Web App keeps the files both runtimes read in its client. A library
embeds none, since rewording another assembly's strings is the application's choice.

**Beside the running application.** A `.locales` folder beside the application's binary, which
`LocalesPath` names and no build fills, is read last, so a string can change there without building
again.

**Which entry wins.** The assembly's own files are read first, then each locale pack, then the
host's files: each client the application names, then the application's own, then the folder beside
it, each overriding the one before; within each, a file closer to the type wins.

**Another layout.** An assembly that keeps its files elsewhere, or whose root namespace differs
from its name, states it in a `.locale.json` at the project root, which the build embeds and which
the build needs nothing from, since it finds the files by their entry:

```json
{ "paths": ["$RootDir/$AnchorName.$Culture$Extension"] }
```

| Key | What it says |
| --- | --- |
| `rootNamespace` | The root namespace a type's name is counted from. |
| `rootDir` | The folder beneath the project root where the files begin, written with `/`. |
| `localesDir` | The name of the folder of locale files; `.locales` by default. |
| `paths` | The paths the files are read by, in order, replacing the default ones. |

A host's folder follows the type's name whatever layout the assembly chose.

## Locale packs

A locale pack is an assembly of locale files alone, carrying one language for other assemblies'
types and shipped as its own package. An application installs a language by referencing the pack's
package; the package records the pack in the application at build time, and `AddJsonLocalization`
reads the packs recorded.

A project becomes a pack by declaring `IsLocalePack`, naming its culture in `LocalePackCulture`,
and referencing this package, whose build does the rest:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net11.0</TargetFramework>
        <IsLocalePack>true</IsLocalePack>
        <LocalePackCulture>fr</LocalePackCulture>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="formaui-net.Framework.Localization" Version="1.0.0-preview.2" />
    </ItemGroup>

</Project>
```

Its files are laid out as a host's folder is, a folder per assembly:

```text
Forma.Blazor/Components/Bars/fr.json      { "FormaSelectionToolbar": { "Clear Selection": "Effacer la sélection" } }
Forma.Blazor.Composer/fr.json             { "FormaComposer": { "Bold": "Gras" } }
```

The build refuses a file of any culture the pack does not declare. An application references a
pack directly; a class library or a test that references one names it in `LocalePacks` instead.

## Settings

`LocalizationOptions` carries the settings every backend shares, passed to its registrar:

| Setting | Default | What it does |
| --- | --- | --- |
| `SourceLanguage` | `en` | The language the keys are written in. |
| `RequireMatchingLocalization` | `false` | Throws `LocalizationNotFoundException` for a key no locale file localizes, so a development build finds what is missing. |
| `LocalesPath` | `.locales` | The host's folder of locale files beside the running application, relative to its base directory; empty reads none. |
| `LocalePacks` | none | Locale packs read beside the ones the application's build recorded. |
| `HostLocaleAssemblies` | none | Assemblies carrying a host's embedded files, read beside the ones the application's build recorded. |

## Reference

| Type | What it is |
| --- | --- |
| `IStringLocale<T>` | The strings of type `T`: `this[name]`, `this[name, values]`, `Resolve`, `GetAllStrings`, `WithCulture`. |
| `IStringLocaleFactory` | Creates a locale for a type, `Create(Type)`, or for a base name in an assembly, `Create(string, string)`. |
| `LocalizationOptions` | The settings above. |
| `LocalizedText` | A localized string: `Value`, `Name`, `ResourceNotFound`, `SearchedLocation`. |
| `LocalizedPattern` | A localized format with its holes open: `Format`, `Arguments`, `EnumerateSegments()`. |
| `LocalizedSegment` | One run of a format: literal `Text`, or a hole with a `Name`, `Alignment` and `Format`. |
| `LocalizedFormat` | Pairs values with a format's holes, `Bind`, and renders it, `Render`, without a locale. |
| `CommonPluralRuleSet` | The CLDR plural rules behind `IPluralRuleSet`, in `Forma.Framework.Localization.Language`. |

The `Forma.Framework.Localization.Files` and `Forma.Framework.Localization.Parser` namespaces read
locale files and their syntax by the same rules the runtime uses, for a tool that checks them.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
The CLDR plural rules the package embeds are used under the Unicode License, whose notice ships in
[`THIRD-PARTY-NOTICES.md`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Localization/THIRD-PARTY-NOTICES.md).
