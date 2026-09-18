# Forma.Framework.Localization

The localization contract `Forma.Blazor` reads every string through: a locale that localizes a
key, the value types a localized string comes back as, the settings every backend shares, the
layout locale files follow, and the plural rules a localized format is evaluated with. A key is the
source text itself, so a key with no translation renders as written.

The package is backend-agnostic. A backend registers `IStringLocaleFactory` and `IStringLocale<T>`;
`Forma.Framework.Localization.Json` is the backend Forma ships, reading JSON files embedded in the
assembly whose types they localize.

## Install

```pwsh
dotnet add package 2Site.Forma.Framework.Localization
```

A host installs a backend alongside it, `Forma.Framework.Localization.Json` unless it brings its
own. Where no backend is registered, `NullStringLocaleFactory.Instance` stands in: every locale it
creates returns each key as its own text, with its holes filled and its plural forms resolved for
the source language.

## The contract

| Type | What it is |
| --- | --- |
| `IStringLocale` | Localized strings for one resource base name: `this[name]`, `this[name, values]`, `Resolve`, and `GetAllStrings`. |
| `IStringLocale<T>` | The same, for the base name derived from `T`. Adds no members. |
| `IStringLocaleFactory` | Creates a locale for a type, `Create(Type)`, or for a base name under a location, `Create(string, string)`. |
| `NullStringLocaleFactory` | The factory standing in where none is registered; `Instance` is the one instance. |
| `LocalizationOptions` | The settings every backend shares: `SourceLanguage`, `RequireMatchingLocalization`, and `LocalesPath`. |
| `LocalizationNotFoundException` | Thrown when a key has no translation and `RequireMatchingLocalization` is on. |
| `LocalizedText` | A localized string, `Value`, with the key it was looked up under, `Name`, whether the lookup fell back to the key, `ResourceNotFound`, and the base name searched, `SearchedLocation`. Converts implicitly to `string`. |
| `LocalizedPattern` | A localized format, `Format`, with its plural forms resolved and its holes still open, and the `Arguments` the holes name. |
| `LocalizedArgument` | A value with the `Name` a hole refers to it by; `Unnamed` holds the value an expression naming no argument reads. |
| `LocalizedFormat` | Pairs values with a format's holes, `Bind`, and renders a format once they are paired, `Render`. |
| `LocalizedSegment` | One run of a format: literal `Text`, or a hole with a `Name`, an `Alignment`, and a `Format` specifier; `Parse` walks any format. |
| `LocalizedSegmentEnumerator` | Walks a format as segments, slicing rather than copying. |
| `LocaleName` | Decides what may name an argument: `IsValid` accepts a plain identifier. |

## Looking a string up

A type takes the locale for itself and looks a key up by the source text:

```csharp
using Forma.Framework.Localization;

public sealed class UploadStatus(IStringLocale<UploadStatus> locale)
{
    public string Saved
        => locale["Saved"];

    public string Progress(int stored, int total)
        => locale["Stored {Stored} of {Total}.", stored, total];
}
```

In a Razor component the same locale is injected, `@inject IStringLocale<UploadPage> L`, and a
component deriving from `Forma.Blazor`'s bases already has it as `L`. A locale for a type chosen at
runtime comes from the factory, `factory.Create(typeof(UploadStatus))`.

A lookup returns a `LocalizedText`, which converts to `string` on its own and reports what happened:

```csharp
var text = locale["Save"];

text.Value              // "Guardar" under es; "Save" where no translation exists
text.ResourceNotFound   // true when the lookup fell back to the key
text.SearchedLocation   // the base name that was searched

locale.GetAllStrings(includeParentCultures: true)
```

## Filling holes

A hole is `{Name}`, optionally `{Name,alignment}` and `{Name:specifier}`, where the name is a plain
identifier. Values pair with the key's holes by position, in the order the holes first appear:

```csharp
locale["Stored {Stored} of {Total}.", stored, total]
```

| Rule | |
| --- | --- |
| A lookup takes one value per distinct hole | any other count throws `ArgumentException` |
| A repeated hole is still one hole | `"{N} of {N}"` takes one value |
| A key with no holes takes at most one value | which an expression naming no argument reads |
| A hole with no argument renders as written | `{Total}` appears literally |

A translation refers to the holes by name, so it may order them however reads best. A value is
passed as an `object` rather than rendered first, so the format being rendered decides its
specifier and alignment: `"Ratio {Ratio:P1}"` renders its value with `P1` where no translation
exists, and a translation may write `{Ratio}` or `{Ratio:P2}` instead.

A literal brace is doubled, in keys and in locale files alike: `"Use {{curly}}"` renders as
`Use {curly}`, and its translation writes `{{` too.

## Plural forms

A plural expression selects one of its fields by a count:

```text
[$ [Name] plural; category: text; …; $]
```

`[Name]` is the argument holding the count, and `#` in a field is that count formatted for the
culture on screen. Fields are `category: text` pairs separated by `; `, and the trailing `;` is
required. The categories are CLDR's, `zero`, `one`, `two`, `few`, `many` and `other`, and a
language carries only the ones it distinguishes: English uses `one` and `other`, Hebrew adds `two`,
Arabic uses all six.

```json
"{Files} files": "[$ [Files] plural; one: # file; other: # files; $]"
```

The name is left out when the count is the only value, and that form takes exactly one value:

```json
"[$ plural; one: # file; other: # files; $]": "[$ plural; one: קובץ אחד; two: שני קבצים; other: # קבצים; $]"
```

```csharp
locale["[$ plural; one: # file; other: # files; $]", count]   // "1 file", "3 files"
```

The category comes from `IPluralRuleSet`. A field the translation omits falls back to `other`, and
an expression with neither renders as empty text. `#` keeps the digits the count was written with,
so `0.5` does not round and `1000000` is grouped for the culture. A malformed expression is left as
written, and so is an expression named for another keyword.

`#` means the count only inside a selected field, so `"Issue #42 in C#"` is untouched. Inside a
field a literal `#` is doubled, `##`; elsewhere `##` stays as written.

Expressions use brackets and holes use braces, so an expression and a hole can sit in one format:
the expressions are resolved first, and every other hole is filled after.

## Patterns and segments

`Resolve` returns the localized format with its plural forms selected and its holes still open,
so a caller can render literal text and holes differently, filling a hole with rendered content
rather than text:

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

Segments run over the localized format, so they come in the order the translation writes them.
`LocalizedSegment.Text` and `Name` are `ReadOnlySpan<char>` sliced from the format and the
enumerator is a `ref struct`, so walking allocates nothing. A doubled brace arrives as its own
single-character run: `"Use {{curly}}"` yields `Use `, `{`, `curly`, `}`.

`LocalizedSegment.Parse` walks any format the same way, and `LocalizedFormat` renders one without
a locale: `Bind` pairs values with the format's holes and `Render` writes them in a culture.

## Plural rules

The `Forma.Framework.Localization.Language` namespace carries the rules a plural expression is
evaluated with:

| Type | What it is |
| --- | --- |
| `IPluralRuleSet` | Decides which plural category a count falls into for a locale: `Select(locale, operands)`, or `Select(culture, value)` falling back to `Other`. |
| `PluralCategory` | The six CLDR categories: `Zero`, `One`, `Two`, `Few`, `Many`, `Other`. |
| `PluralOperands` | The CLDR operands a rule reads, `n`, `i`, `v`, `w`, `f`, `t` and `e`; `From(decimal)` derives them from a number. |
| `CommonPluralRuleSet` | The CLDR 48 cardinal rules for every locale it publishes; `Default` is the shared instance. |
| `PluralExpressionEvaluator` | Replaces every plural expression in a format with the field its count selects. |

The operands read how a number was written rather than its value alone, so `1` and `1.0` can
differ: `1.0` is `one` in Spanish and `other` in English.

Rules are keyed by locale, and the fullest identifier wins before falling back a subtag at a time:
`pt-PT` finds Portugal's rules, `es-CO` falls back to Spanish, and `0` is `one` in Brazil but
`other` in Portugal. `Select` returns `null` for a locale the set carries no rules for.

```csharp
using Forma.Framework.Localization.Language;

CommonPluralRuleSet.Default.Select(CultureInfo.GetCultureInfo("he"), 2)   // PluralCategory.Two
```

A host wanting different rules implements `IPluralRuleSet` itself.

## Settings

`SourceLanguage` names the language the keys are written in, and defaults to `en`. A key with no
translation resolves its plural forms under that language rather than the one on screen, while
its count is still formatted for the reader.

`RequireMatchingLocalization` turns a missing translation from a fallback into a
`LocalizationNotFoundException`, and defaults to `false`. `GetAllStrings` never throws under it,
so a host can survey what is untranslated.

`LocalesPath` names the folder the host keeps its own locale files in, `.locales` beside the
application by default, resolved against the application's base directory when relative; empty
reads none. Where an assembly keeps its own embedded files is no option's to say: the paths a
`.locali.json` among them states place them.

## How a file is found

A type's translations are read from the assembly's root down to the type. The root is where the
assembly's files begin, the project folder its root namespace stands for; each folder beneath it
is one segment of the type's full name, and each such name, the type's own included, is an
**anchor** a file can stand for. Generic arity and nested-type markers are dropped: `Grid<T>`
reads as `Grid`, `Outer+Inner` as `Outer.Inner`, and a closed generic reads through its open
definition. A type whose namespace does not start with the root namespace is read by its full
name, and a type outside any namespace belongs to the root. Every `LocalizedText` reports the
type's full name as `SearchedLocation`, qualified by the assembly's name for a type outside any
namespace.

Every folder keeps its translations in a `.locales` folder of its own, one file per culture named
for the culture alone, and sections complete the rest of the type's name. Each of these, shown
with the JSON backend's extension, localizes `FormaToolbar` in `Forma.Blazor.Components.Bars`:

```text
.locales/es.json                                { "Components.Bars.FormaToolbar": { "Bold": "Negrita" } }
Components/.locales/es.json                     { "Bars.FormaToolbar": { "Bold": "Negrita" } }
Components/Bars/.locales/es.json                { "FormaToolbar": { "Bold": "Negrita" } }
Components/Bars/FormaToolbar/.locales/es.json   { "Bold": "Negrita" }
```

A section's key may spell several segments at once or one at a time, so
`"Components": { "Bars.FormaToolbar": { … } }` reads the same as the first line. A file holds only
the keys it changes, and a file closer to the type wins over one higher up.

That layout is a path written in tokens, `$RootDir/$AnchorName/$LocalesDir/$Culture$Extension`,
and a `.locali.json` states other paths for the files beneath it. A project keeping each file
beside the folder or type it localizes, as `Components/Bars.es.json` or
`Components/Bars/FormaToolbar.es.json`, writes:

```json
{ "paths": ["$RootDir/$AnchorName.$Culture$Extension"] }
```

`localesDir` renames the `.locales` folder beneath it, and the outermost `.locali.json` an
assembly embeds marks its root, naming the root namespace with `rootNamespace` where it is not
the assembly's name and the folder its files begin in with `rootDir`.

Any node may hold a `$shared` section, whose entries every type reached through that node gets.
Here the assembly's file shares `Bold` and `Italic` with every type it reaches, `Composer` shares
`Undo` with the editors alone, and `FormaComposer` words `Bold` its own way:

```json
{
  "$localization": "SchemaVersion: 1.0.0; Culture: es;",
  "$shared": { "Bold": "Negrita", "Italic": "Cursiva" },
  "Components": {
    "Composer": {
      "$shared": { "Undo": "Deshacer" },
      "FormaComposer": { "Bold": "Negrita Fuerte" },
      "RichTextEditor": { "FormaRichTextEditor": { } }
    }
  }
}
```

A `$shared` section holds translations alone. A type's own entry wins over a shared one, a deeper
node's shared entry wins over a higher node's, and a file closer to the type wins over a shared
entry higher up, as it wins over any other.

`$tools` is reserved beside it for tools, each keeping its own entries under keys naming it, such
as `"$tools": { "crowdin.id": 1 }`; the runtime reads nothing from it.

A key shaped like a reserved entry, `$` followed by one lowercase word and nothing else, is the
runtime's, and a file naming one it does not define, such as `$import`, fails to load. A doubled
sign escapes the shape: `$$price` is the key `$price`, in a file and at a call site alike, read
with exactly one `$` dropped and never interpreted again, so `$$shared` is the ordinary key
`$shared`. Any other key starting with `$`, such as `$`, `$5 off`, `$USD` or `$camelCase`, is
ordinary text and needs no escape. Translation values are never read for the shape.

A file named for its culture alone stays in the assembly itself. The build moves a file whose name
ends in its culture, such as `Bars.es.json`, into that culture's satellite assembly unless its item
sets `WithCulture="false"`, and the runtime reads it from there under the same name.

## Locale files from the host

A host lays its own files in a `.locales` folder beside the application, to reword a string an
assembly ships or to add a language it never had. `LocalesPath` names the folder.

The folder holds a folder for each assembly, which is that assembly's root, laid out as the
assembly's own files are. Each of these, shown with the JSON backend's extension, localizes
`FormaComposer` in `Forma.Blazor.Components.Composer`:

```text
.locales/Forma.Blazor/.locales/es.json                                     { "Components.Composer.FormaComposer": { "Bold": "Negrita" } }
.locales/Forma.Blazor/Components/.locales/es.json                          { "Composer.FormaComposer": { "Bold": "Negrita" } }
.locales/Forma.Blazor/Components/Composer/FormaComposer/.locales/es.json   { "Bold": "Negrita" }
```

The host's folder is itself a folder of locale files, so a host file may also sit directly in the
folder it localizes: `.locales/Forma.Blazor/es.json` is the root's file and
`.locales/Forma.Blazor/Components/es.json` the `Components` folder's. The outer form speaks for the
host's root as a whole and the inner one for the folder alone, and where both stand for one
folder, the file in the folder's own `.locales` wins.

The same precedence applies, and a host's entry wins over the assembly's own at the same position,
so a host file holds only the keys it changes and every other key keeps the assembly's translation.

A host file completes a type's name from the assembly's name, so the host's layout follows the
namespace rather than the embedding. For an assembly whose root namespace is not its name, a
`.locali.json` in the assembly's folder names the root, as
`{ "rootNamespace": "Forma.Framework.Resources.Office" }`, and the paths and sections then complete
the name from there: beneath that file,
`.locales/Forma.AddOns.Resources.Office/.locales/es.json` with an `OfficeResourceProvider` section
localizes `Forma.Framework.Resources.Office.OfficeResourceProvider`. Where no settings file names a
root, a type whose namespace does not start with the assembly's name is laid out by its full name.

The folder has to reach the application's output, and a project marks it as content copied to the
output directory, web project or not: the SDK's own globs pass over a folder whose name starts
with a dot.

```xml
<ItemGroup>
    <Content Include=".locales\**\*.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

## Reading files from a tool

The `Forma.Framework.Localization.Files` namespace is the layout above as public types, so a tool
checks files by the rule the runtime reads them with:

| Type | What it is |
| --- | --- |
| `LocaleSource` | Where locale files are read from: `Read(assemblyName, typeName, cultureName)` reads a type's files from its root down to the type, placing each file the store lists by the paths in force where it sits, over a store a derived class lists and opens, and a derived class asks `GetAnchorNames` which anchors a file stands for. |
| `EmbeddedLocaleSource` | The files embedded in one assembly, or in a culture's satellite assembly. |
| `HostLocaleSource` | The folder a host keeps its own locale files in. |
| `ILocaleFileFormat` | A format files are written in: its `Extension`, and how one file is `Read` into a `LocaleFile`. |
| `LocaleFile` | One file as read: its `Root` section; `GetEntries` lists every translation with the type name its sections complete. |
| `LocaleSection` | One node of a file: its `Sections`, its `Entries`, and its `SharedEntries`. |
| `LocaleFileEntry` | One translation a file declares, with the `TypeName` its sections complete. |
| `LocaleSourceEntry` | One translation a source read for a type, with the `FileName` and `TypeName` it came from. |
| `LocalizationSchema` | The shape of the metadata entry every file carries: its `Marker`, `CurrentVersion`, `RequiredFields`, `OptionalFields`, the reserved keys, and `IsReservedShape` and `Unescape` for keys. |
| `LocalizationMetadata` | `Validate` checks that a file's metadata entry lets the file be read. |
| `LocalizationMetadataException` | Thrown when a file's metadata does not let it be read. |
| `LocalePath` | The path of a locale file written in tokens, which `Parse` reads; `Defaults` are the paths a source reads by unless a setting states others, and `HostDefaults` the ones a host's folder reads by. |
| `LocalePathBinder` | Builds a file's name from a path for an anchor, `GetName`, in one source and one culture. |
| `LocalePathMatcher` | Reads the anchor a file's name stands for under a path, `TryGetAnchor`, in one source and one culture. |
| `LocalePathValues` | What a path's tokens stand for apart from the anchor: the assembly, the root, the folder of locale files, the culture and the extension. |
| `LocaliSettings` | What a `.locali.json` file says about the files beneath it; `Read` takes the keys the runtime owns and passes over the linter's. |
| `LocaliSettingsException` | Thrown when a settings file cannot be read or a key the runtime owns is malformed. |

The `Forma.Framework.Localization.Parser` namespace reads the syntax inside a string:
`LocaleExpressionParser` reads the `[$ … $]` expressions in a format and `LocaleFieldParser` the
`name: value;` pairs of an expression body or a metadata entry, and `LocalePathParser` a locale
path's literal runs and tokens, each returning the spans it read and the diagnostics it raised.

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
