# Forma.Framework.Localization

A localization contract for .NET libraries and applications. A type looks a string up by its
text in the source language, English unless `SourceLanguage` says otherwise, so a string no locale
file localizes renders as written, and localized strings are found by the name of the type asking.
The package carries the hole, plural and gender syntax a string is written in, the CLDR plural rules, and
the build that makes a project a locale pack.

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
same two contracts. Code written against Microsoft's `IStringLocalizer` reads the same strings through
[`formaui-net.Framework.Localization.StringLocalizerAdapter`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Localization.StringLocalizerAdapter/README.md). `NullStringLocaleFactory.Instance` is a factory whose locales return every key
as its own text, with holes filled and plural and gender forms resolved in the source language, and
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
`{Ratio:P1}`. Values pair by position with the names the key uses, its holes and the arguments its
expressions name, in the order each name first appears, one value per distinct name; any other count
throws `ArgumentException`. A localized string refers to the holes by name, so it may reorder them,
and it may choose its own specifier. A literal brace is doubled, `{{`, in keys and localized strings
alike.

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

### Gender forms

A gender expression selects one of its fields by whom the string is about:

```text
[$ [Rider] gender; female: Escaladora; other: Escalador; $]
```

`[Rider]` names the argument holding the gender, and is left out when the gender is the only value.
The fields are `female`, `male` and `other`, and each language writes the ones its words change for:
Spanish writes `female` beside `other` for a climber, and a language whose words don't change writes
`other` alone. A field a localized string omits falls back to `other`.

The argument is an enum value or a string, and selects the field its name spells, whatever its
case: `Gender.Female` and `"female"` select `female`. A null, or a name without a field of its own,
such as `NonBinary`, selects `other`.

A key whose gender is its only value carries no expression where its words are the same for
everyone, and its localization reads the value unnamed. A key with other values carries the
expression with `other` alone, which names the argument, so the values pair with `[Rider]` and
`{Name}` in the order they first appear:

```csharp
locale["Climber", rider.Gender]
locale["[$ [Rider] gender; other: {Name} is the best climber; $]", rider.Gender, rider.Name]
```

```json
"Climber": "[$ gender; female: Escaladora; other: Escalador; $]",
"[$ [Rider] gender; other: {Name} is the best climber; $]": "[$ [Rider] gender; female: {Name} es la mejor escaladora; other: {Name} es el mejor escalador; $]"
```

### Rendering holes as markup

`Resolve` returns the localized format with its plural and gender forms selected and its holes still open, so
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

### Text carried as data

A registry, a menu's entries or a list of sections, carries a label it can't localize where it is
built, since nothing has a culture yet and the reader may switch language later. A `LocalizableText`
carries what the lookup needs instead: the type whose locale holds the key, the key, and the values for
its holes, raw. `Localize` looks it up when the text is shown, in the current cultures or a culture
state's, and a value that is itself a `LocalizableText` is localized in turn:

```csharp
using Forma.Framework.Localization;

public sealed record Section(string Address, LocalizableText Title);

public static class Sections
{
    public static IReadOnlyList<Section> All { get; } =
    [
        new("sports", LocalizableText.For<SectionMenu>("Sports")),
        new("results", LocalizableText.For<SectionMenu>("{Count} results today", 12)),
    ];
}
```

```razor
@inject IStringLocaleFactory Locales
@inject ICultureState CultureState

@foreach (var section in Sections.All)
{
    <a href="@section.Address">@Locales.Localize(section.Title, CultureState)</a>
}
```

The text holds no string and no culture, so it reads the language in force when it is shown. Two
`LocalizableText`s are equal where their sources, keys and values are.

## Where locale files live

**One canonical name for every file.** The runtime reads a locale file's name once, into its
canonical name: the culture the file is for and the parts of the name it spells, a folder and a dot
alike, with the folder of locale files left out, `.locales` unless the assembly's `localesDir` names
another. Past the root namespace, the parts name the namespace or the type the file's sections
complete. Nothing after that reading looks at a path again; a file keeps the name its source knows it
by, its resource name in an assembly or its path beside the application, only to name it in a
message. In the project of `Contoso.App`:

| File | Embedded as | Canonical name |
| --- | --- | --- |
| `Uploads/.locales/he.json` | `Contoso.App/Uploads/.locales/he.json` | `he` · `Contoso.App.Uploads` |
| `Uploads.he.json` | `Contoso.App/Uploads.he.json` | `he` · `Contoso.App.Uploads` |
| `.locales/he/Uploads.json` | `Contoso.App/.locales/he/Uploads.json` | `he` · `Contoso.App.Uploads` |
| `.locales/Uploads/he.json` | `Contoso.App/.locales/Uploads/he.json` | `he` · `Contoso.App.Uploads` |
| `Uploads/.locales/UploadStatus.he.json` | `Contoso.App/Uploads/.locales/UploadStatus.he.json` | `he` · `Contoso.App.Uploads.UploadStatus` |
| `Uploads/UploadStatus.he.json` | `Contoso.App/Uploads/UploadStatus.he.json` | `he` · `Contoso.App.Uploads.UploadStatus` |
| `.locales/he/Uploads/UploadStatus.json` | `Contoso.App/.locales/he/Uploads/UploadStatus.json` | `he` · `Contoso.App.Uploads.UploadStatus` |
| `.locales/he.json` | `Contoso.App/.locales/he.json` | `he` · `Contoso.App` |
| `.locales/he/he.json` | `Contoso.App/.locales/he/he.json` | `he` · `Contoso.App` |
| `.locales/he/Uploads.he.json` | `Contoso.App/.locales/he/Uploads.he.json` | `he` · `Contoso.App.Uploads` |
| `.locales/es-419/Uploads.json` | `Contoso.App/.locales/es-419/Uploads.json` | `es-419` · `Contoso.App.Uploads` |
| `Strings/he/Uploads.json` | `Contoso.App/Strings/he/Uploads.json` | `he` · `Contoso.App.Strings.Uploads` |
| `.locales/ES/Uploads.json` | `Contoso.App/.locales/ES/Uploads.json` | none: `ES` isn't how .NET writes a culture |

A folder other than the folder of locale files stays part of the name, as `Strings` does. Files with
one canonical name are read together, as one file would be, `Uploads/UploadStatus.he.json` and
`Uploads.UploadStatus.he.json` among them.

**One entry for every string.** A file's sections, at its canonical name, are read into entries, and
the runtime works on entries alone: which text a lookup finds, which text wins and what is a conflict
never depend on where a file sat or what it was named. An entry is a culture, an assembly, a type and
a key, with the text the type shows for that key in that culture:

```text
culture · assembly · type · key → text
he · Contoso.App · Contoso.App.Uploads.UploadStatus · "Saved" → "נשמר"
```

Each of these files, in the project of `Contoso.App`, is read into that one entry, so a project lays
its files out as it likes:

```text
Uploads/.locales/he.json                   { "UploadStatus": { "Saved": "נשמר" } }
Uploads/.locales/UploadStatus.he.json      { "Saved": "נשמר" }
Uploads.he.json                            { "UploadStatus": { "Saved": "נשמר" } }
.locales/he/Uploads.json                   { "UploadStatus": { "Saved": "נשמר" } }
.locales/he/Uploads/UploadStatus.json      { "Saved": "נשמר" }
.locales/he.json                           { "Uploads": { "UploadStatus": { "Saved": "נשמר" } } }
```

The same entry arrives from elsewhere too, each holding `{ "UploadStatus": { "Saved": "נשמר" } }`;
only its source differs, which decides precedence, as *Which text wins* below sets out:

| File | Where it sits |
| --- | --- |
| `Contoso.App/Uploads/he.json` | In a locale pack |
| `.locales/he/Contoso.App/Uploads.json` | In an application that rewords `Contoso.App`'s strings |
| `strings-he/Uploads.json` | In `Contoso.App`, read by the path `$RootDir/strings-$Culture/$AnchorName$Extension` its `.locale.json` states |

**The default reading.** The first part of a name that is a culture, written as .NET names it, `he`
or `es-419` but never `ES`, is the file's culture, whether it is a folder or part of the name, and a
part repeating it is the culture again, so a culture folder holds its root's file as `he/he.json`.
Every other part goes on the type's name, past the project's root namespace, and a file's sections
spell the rest of it. A regional culture's files, such as those for `es-419`, hold only what differs
from its parent's.

**Embedded by the build.** The build embeds every locale file it finds by the `$localization` entry
each one carries, wherever it sits in the project, outside the build output and outside a folder
whose name starts with a dot but the folder of locale files, under the project's root namespace, a
`/` and the file's path, with `/` between folders on every platform, so a project names none of them
and a culture folder such as `es-419` keeps its name. A file the project embeds through an item of
its own keeps the name that item gives it. The SDK's naming makes every folder a dot, which the
default reading still reads; a stated path and an application's files for another assembly match
folders, so a file either of them reads is named with its folders, as the build names one. A file an
item in the project file copies beside its output stays a file there.

**Another assembly's strings.** An application rewords a string, or adds a language the library
never shipped, in its own files, under a name that starts with the assembly's name and goes on with
the type's namespace past that assembly's root namespace:

```text
.locales/Forma.Blazor/Components/Bars/he.json    { "FormaSelectionToolbar": { "Clear Selection": "בטל בחירה" } }
.locales/fr/Forma.Blazor/Components/Bars.json    { "FormaSelectionToolbar": { "Clear Selection": "Effacer la sélection" } }
```

They are read into the library's own entries, from the application's source:

```text
he · Forma.Blazor · Forma.Blazor.Components.Bars.FormaSelectionToolbar · "Clear Selection" → "בטל בחירה"
fr · Forma.Blazor · Forma.Blazor.Components.Bars.FormaSelectionToolbar · "Clear Selection" → "Effacer la sélection"
```

An application, a WebAssembly client, a server built on the Web SDK or a Blazor Hybrid application,
reads these from its own assembly, so a browser and an application MAUI packages, with no folder to
read, read them too. A Web App's server reads its client's as well: its build records each client it
references that embeds locale files with `LocaleReferenceAttribute`, so a Web App keeps the files
both runtimes read in its client. A library's files are read for its own types alone, since
rewording another assembly's strings is the application's choice.

**Beside the running application.** A `.locales` folder beside the application's binary, which
`LocalesPath` names and no build fills, is read last, so a string can change there without building
again. It is read by the same rule, with folders where an embedded name has dots.

**Which text wins.** For one type, one key and one reader's culture:

1. **The culture.** The reader's culture, then each of its parents: `es-419`, then `es`. A more
   specific culture wins, so an `es-419` reader sees a library's `es-419` text over an
   application's `es` one.
2. **The source.** The type's own assembly, then each locale pack, then each client the
   application names, then the application's own files, then the folder beside it, each
   overriding the one before.
3. **Within one source,** a type's own entry wins over a `$shared` one, and two files giving one
   key different texts are a conflict: the file whose name sorts last, ordinally, is shown, and
   the conflict is logged once as a warning naming both files.

**Another layout.** An assembly that keeps its files in a layout the default reading has no rule
for, or whose root namespace differs from its name, states it in a `.locale.json` at the project
root, which the build embeds and which the build needs nothing from, since it finds the files by
their entry. A path is notation, as the default reading is: it says where a file sits and how its
name spells a type, and what a file read by it holds is the same entries, so `strings-he/Uploads.json`
read by this path holds the entry `.locales/he/Uploads.json` does by default:

```json
{ "paths": ["$RootDir/strings-$Culture/$AnchorName$Extension"] }
```

| Key | What it says |
| --- | --- |
| `rootNamespace` | The root namespace a type's name is counted from. |
| `rootDir` | The folder beneath the project root where the files begin, written with `/`. |
| `localesDir` | The name of the folder of locale files; `.locales` by default. |
| `paths` | The paths the files are read by, in order, replacing the default reading. |

An application's files for another assembly follow that assembly's names, whatever layout the
assembly chose.

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
        <PackageReference Include="formaui-net.Framework.Localization" Version="1.0.0-preview.17" />
    </ItemGroup>

</Project>
```

Its files are laid out as an application's files for another assembly are, each name starting with
the assembly's name, its culture a folder or part of the name:

```text
Forma.Blazor/Components/Bars/fr.json      { "FormaSelectionToolbar": { "Clear Selection": "Effacer la sélection" } }
Forma.Blazor.Composer/fr.json             { "FormaComposer": { "Bold": "Gras" } }
```

The build refuses a file of any culture the pack does not declare, or of none, reading a file's
culture as the runtime does. An application references a pack directly, and a test hosting the
application, as `WebApplicationFactory` does, reads the packs the application references. A class
library, or a test rendering components without the application, names its packs in `LocalePacks`
instead.

## Settings

`LocalizationOptions` carries the settings every backend shares, passed to its registrar:

| Setting | Default | What it does |
| --- | --- | --- |
| `SourceLanguage` | `en` | The language the keys are written in. |
| `RequireMatchingLocalization` | `false` | Throws `LocalizationNotFoundException` for a key that the UI culture and the cultures it falls back to don't hold an entry for, so a test fails where a string is missing. A culture of the source language, and the invariant culture, show the key itself, which satisfies it. |
| `Diagnostics` | none | The checks run on every lookup, a `LocalizationDiagnostics`, which log each key without an entry in the lookup's culture and in the `Cultures` it names, as `Forma.Framework.Localization.Json` sets out. |
| `LocalesPath` | `.locales` | The host's folder of locale files beside the running application, relative to its base directory; empty reads none. |
| `LocalePacks` | none | Locale packs read beside the ones the application's build recorded. |
| `HostLocaleAssemblies` | none | Applications whose embedded files are read for other assemblies' strings, beside the ones the application's build recorded. |

## Reference

| Type | What it is |
| --- | --- |
| `IStringLocale<T>` | The strings of type `T`: `this[name]`, `this[name, values]`, `Resolve`, `GetAllStrings`, `WithCulture`. |
| `IStringLocaleFactory` | Creates a locale for a type, `Create(Type)`, or for a base name in an assembly, `Create(string, string)`. |
| `LocalizationOptions` | The settings above. |
| `LocalizationDiagnostics` | The checks `Diagnostics` turns on: `Cultures`, the cultures every lookup is checked in beside its own. |
| `LocalizedText` | A localized string: `Value`, `Name`, `ResourceNotFound`, `SearchedLocation`. |
| `LocalizedPattern` | A localized format with its holes open: `Format`, `Arguments`, `EnumerateSegments()`. |
| `LocalizedSegment` | One run of a format: literal `Text`, or a hole with a `Name`, `Alignment` and `Format`. |
| `LocalizedFormat` | Pairs values with the names a format uses, `Bind`, and renders it, `Render`, without a locale. |
| `CommonPluralRuleSet` | The CLDR plural rules behind `IPluralRuleSet`, in `Forma.Framework.Localization.Language`. |

The `Forma.Framework.Localization.Files` and `Forma.Framework.Localization.Parser` namespaces read
locale files and their syntax by the same rules the runtime uses, for a tool that checks them.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
The CLDR plural rules the package embeds are used under the Unicode License, whose notice ships in
[`THIRD-PARTY-NOTICES.md`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Localization/THIRD-PARTY-NOTICES.md).
