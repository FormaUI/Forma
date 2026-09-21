# Forma.Framework.Localization

The localization contract `Forma.Blazor` reads every string through. A key is the source text
itself, so a key with no translation renders as written; a translation is looked up by the type
that asks for it, from files laid out by that type's name.

The package is backend-agnostic. A backend registers `IStringLocaleFactory` and `IStringLocale<T>`;
`Forma.Framework.Localization.Json` is the backend Forma ships, reading JSON files embedded in the
assembly whose types they localize and JSON files the host lays beside the application.

## Install

```pwsh
dotnet add package Forma.Framework.Localization
```

A host installs a backend alongside it, `Forma.Framework.Localization.Json` unless it brings its
own. Where no backend is registered, `NullStringLocaleFactory.Instance` stands in: every locale it
creates returns each key as its own text, with its holes filled and its plural forms resolved for
the source language.

## How It Works?

Everything below is one type, `Forma.Blazor.Components.Bars.FormaToolbar`, its key `Bold`, and
the culture `he`. The files are shown with the JSON backend's extension, and each opens with a
`$localization` entry, left out here for brevity.

**The library's own files.** Every folder keeps its translations in a `.locales` folder of its
own, holding two kinds of file: the folder's file, named for the culture alone, whose sections
spell the rest of the type's name, and a type's own file, named for the type, whose entries are
the type's. A folder's file may sit at any depth between the assembly's root and the type's
namespace:

```text
src/Forma.Blazor/.locales/he.json                                   { "Components.Bars.FormaToolbar": { "Bold": "מודגש" } }
src/Forma.Blazor/Components/Bars/.locales/he.json                   { "FormaToolbar": { "Bold": "מודגש" } }
src/Forma.Blazor/Components/Bars/.locales/FormaToolbar.he.json      { "Bold": "מודגש" }
```

A folder is a namespace, never a type, so a file always says what it localizes by its name or its
sections. In full, the folder's file above is:

```json
{
  "$localization": "SchemaVersion: 1.0.0; Culture: he;",
  "FormaToolbar": {
    "Bold": "מודגש"
  }
}
```

A library may prefer another shape for its own files, such as `Components/Bars.he.json` beside
the folder; it states that in a `.locale.json` (see *Where a file may sit*), and nothing below
changes because of it.

**The host's override.** An application rewords a string in a `.locales` folder beside its
binary. The first folder is the assembly's name; beneath it the layout is the type's name as
folders, whatever form the library chose for itself, and the host's folder is already a folder
of locale files, so a file sits directly in the folder it localizes. A host file holds only the
keys it changes:

```text
.locales/Forma.Blazor/he.json                                      { "Components.Bars.FormaToolbar": { "Bold": "עבה" } }
.locales/Forma.Blazor/Components/Bars/he.json                      { "FormaToolbar": { "Bold": "עבה" } }
.locales/Forma.Blazor/Components/Bars/FormaToolbar.he.json         { "Bold": "עבה" }
```

A file a host puts in a folder's own `.locales` instead resolves to the same place:
`.locales/Forma.Blazor/.locales/he.json` says exactly what `.locales/Forma.Blazor/he.json` says, so
a file in both is a duplicate, not an override.

**A language the library never shipped.** The same layout with the new culture's file. A regional
culture may hold only what differs from its parent:

```text
.locales/Forma.Blazor/Components/Bars/fr.json                      { "FormaToolbar": { "Bold": "Gras" } }
.locales/Forma.Blazor/Components/Bars/fr-CA.json                   { "FormaToolbar": { "Bold": "Gras" } }
```

**An assembly whose root namespace is not its name.** The folder is still named for the assembly,
and the type's name is counted from the root namespace the assembly declares in its own
`.locale.json`, `{ "rootNamespace": "Forma.Framework.Resources.Office" }`:

```text
.locales/Forma.AddOns.Resources.Office/es.json                      { "OfficeResourceProvider": { "Open": "Abrir" } }
```

**A file that names its type outright.** A section at a file's top level whose name starts with
`@.` is counted from the assembly's root rather than from where the file sits, so a file anywhere
in the library's tree, or in the host's folder for it, localizes any type of the assembly by
name; a type outside the root namespace is spelled in full:

```text
src/Forma.Blazor/Strings/.locales/he.json                           { "@.Components.Bars.FormaToolbar": { "Bold": "מודגש" } }
```

**Which entry wins.** For one type and one culture, entries are applied in this order, each
overriding the one before: the library's files, then the host's; within each, the file at the
root first and the type's own file last, a rooted section counting where the name it spells
sits; within one file, a shared entry before the type's own. So the deepest host file has the
last word, and a file anywhere holds only the keys it means to set.

**Reaching the output.** The host's folder has to reach the application's output, and the SDK's
own globs pass over a folder whose name starts with a dot, so a project marks it as content, web
project or not:

```xml
<ItemGroup>
    <Content Include=".locales\**\*.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

`LocalesPath` in `LocalizationOptions` names the folder, `.locales` beside the application by
default.

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
text.SearchedLocation   // the type's full name that was searched

locale.GetAllStrings(includeParentCultures: true)
```

### Filling Holes

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

### Plural Forms

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

### Patterns and Segments

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

### Plural Rules

The `Forma.Framework.Localization.Language` namespace carries the rules a plural expression is
evaluated with: `CommonPluralRuleSet.Default` holds the CLDR 48 cardinal rules for every locale
CLDR publishes, and a host wanting different rules implements `IPluralRuleSet` itself.

The operands read how a number was written rather than its value alone, so `1` and `1.0` can
differ: `1.0` is `one` in Spanish and `other` in English.

Rules are keyed by locale, and the fullest identifier wins before falling back a subtag at a time:
`pt-PT` finds Portugal's rules, `es-CO` falls back to Spanish, and `0` is `one` in Brazil but
`other` in Portugal. `Select` returns `null` for a locale the set carries no rules for.

```csharp
using Forma.Framework.Localization.Language;

CommonPluralRuleSet.Default.Select(CultureInfo.GetCultureInfo("he"), 2)   // PluralCategory.Two
```

### Settings

`LocalizationOptions` carries the settings every backend shares:

| Setting | Default | What it does |
| --- | --- | --- |
| `SourceLanguage` | `en` | The language the keys are written in. A key with no translation resolves its plural forms under it rather than the language on screen, while its count is still formatted for the reader. |
| `RequireMatchingLocalization` | `false` | Turns a missing translation from a fallback into a `LocalizationNotFoundException`. `GetAllStrings` never throws under it, so a host can survey what is untranslated. |
| `LocalesPath` | `.locales` | The folder the host keeps its own locale files in, resolved against the application's base directory when relative; empty reads none. |

Where an assembly keeps its own embedded files is no option's to say: the paths its `.locale.json`
states place them.

## Where a File May Live

### Anchors

A type's translations are read from the assembly's root down to the type. The root is where the
assembly's files begin, the project folder its root namespace stands for; each folder beneath it
is one segment of the type's full name, and each such name, the type's own included, is an
**anchor** a file can stand for. A segment is a part of a type's name, so a folder that could not
be one is never read as a segment, and neither is the folder of locale files. A folder is a
namespace, never a type: a type's own file is named for the type among its namespace folder's
locale files, and the linter reports a folder named for a type.

Generic arity and nested-type markers are dropped: `Grid<T>` reads as `Grid`, `Outer+Inner` as
`Outer.Inner`, and a closed generic reads through its open definition. A type whose namespace does
not start with the root namespace is read by its full name, and a type outside any namespace
belongs to the root. Every `LocalizedText` reports the type's full name as `SearchedLocation`,
qualified by the assembly's name for a type outside any namespace.

A section's key may spell several segments at once or one at a time, so
`"Components": { "Bars.FormaToolbar": { … } }` reads the same as `"Components.Bars.FormaToolbar"`.
A section at the file's top level whose key starts with `@.` spells the name from the root
namespace instead of from the file's position, and is read at the anchor it names after the
files placed there; the source finds such sections by reading every file of a culture once, and
a file that cannot be read carries none.

### Paths

The default layout is two paths written in tokens,
`$RootDir/$AnchorName/$LocalesDir/$Culture$Extension` for the folder's file and
`$RootDir/$ParentName/$LocalesDir/$CurrentName.$Culture$Extension` for a file named for a type or
namespace beneath it, and an assembly states other paths for its own files in a `.locale.json`
embedded at its root. A project keeping each file beside the folder
or type it localizes, as `Components/Bars.es.json` or `Components/Bars/FormaToolbar.es.json`,
writes:

```json
{ "paths": ["$RootDir/$AnchorName.$Culture$Extension"] }
```

Paths a file states replace the default ones whole, so a project that wants both lists both, and
the later path wins where two files stand for one anchor.

| Key | What it says |
| --- | --- |
| `rootNamespace` | The root namespace the type's name is counted from, where it is not the assembly's name. |
| `rootDir` | A folder beneath the assembly's root where its files begin, written with `/`. |
| `localesDir` | The name `$LocalesDir` stands for; `.locales` by default. |
| `paths` | The paths the assembly's files are read by, in order, replacing the default ones. |

The file sits at the assembly's root and nowhere else, one per assembly at most, and applies to
every file the assembly embeds; an assembly laid out by the default needs none. It is the
runtime's alone: a tool's configuration never sits in it, and a host lays none, since a host's
folder always follows the type's name whatever the assembly states for itself.

A file named for its culture alone stays in the assembly itself. The build moves a file whose name
ends in its culture, such as `Bars.es.json`, into that culture's satellite assembly unless its item
sets `WithCulture="false"`, and the runtime reads it from there under the same name.

### Shared Entries

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

### Reserved Keys

A key shaped like a reserved entry, `$` followed by one lowercase word and nothing else, is the
runtime's, and a file naming one it does not define, such as `$import`, fails to load. A doubled
sign escapes the shape: `$$price` is the key `$price`, in a file and at a call site alike, read
with exactly one `$` dropped and never interpreted again, so `$$shared` is the ordinary key
`$shared`. Any other key starting with `$`, such as `$`, `$5 off`, `$USD` or `$camelCase`, is
ordinary text and needs no escape. Translation values are never read for the shape.

## Reference

### The Contract

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

### Plural Rules

| Type | What it is |
| --- | --- |
| `IPluralRuleSet` | Decides which plural category a count falls into for a locale: `Select(locale, operands)`, or `Select(culture, value)` falling back to `Other`. |
| `PluralCategory` | The six CLDR categories: `Zero`, `One`, `Two`, `Few`, `Many`, `Other`. |
| `PluralOperands` | The CLDR operands a rule reads, `n`, `i`, `v`, `w`, `f`, `t` and `e`; `From(decimal)` derives them from a number. |
| `CommonPluralRuleSet` | The CLDR 48 cardinal rules for every locale it publishes; `Default` is the shared instance. |
| `PluralExpressionEvaluator` | Replaces every plural expression in a format with the field its count selects. |

### Files, For a Tool

The `Forma.Framework.Localization.Files` namespace is the layout above as public types, so a tool
checks files by the rule the runtime reads them with:

| Type | What it is |
| --- | --- |
| `LocaleSource` | Where locale files are read from: `Read(assemblyName, typeName, cultureName)` reads a type's files from its root down to the type, placing each file the store lists by the paths in force where it sits, over a store a derived class lists and opens; `GetAnchorNames` says which anchors a file stands for. |
| `EmbeddedLocaleSource` | The files embedded in one assembly, or in a culture's satellite assembly. |
| `HostLocaleSource` | The folder a host keeps its own locale files in. |
| `ILocaleFileFormat` | A format files are written in: its `Extension`, and how one file is `Read` into a `LocaleFile`. |
| `LocaleFile` | One file as read: its `Root` section; `GetEntries` lists every translation with the type name its sections complete. |
| `LocaleSection` | One node of a file: its `Sections`, its `Entries`, and its `SharedEntries`. |
| `LocaleFileEntry` | One translation a file declares, with the `TypeName` its sections complete. |
| `LocaleSourceEntry` | One translation a source read for a type, with the `FileName` and `TypeName` it came from. |
| `LocalizationSchema` | The shape of the metadata entry every file carries: its `Marker`, `CurrentVersion`, `RequiredFields`, `OptionalFields`, the reserved keys, `IsReservedShape` and `Unescape` for keys, and `RootedSectionPrefix`, the `@.` a top-level section counts its name from the root with. |
| `LocalizationMetadata` | `Validate` checks that a file's metadata entry lets the file be read. |
| `LocaleFileException` | Thrown when a file cannot be read as a locale file: its content is not what its format reads, or its metadata does not let it be read. Every format throws it. |
| `LocalizationMetadataException` | Thrown when a file's metadata does not let it be read; a `LocaleFileException`. |
| `LocalePath` | The path of a locale file written in tokens, which `Parse` reads; `Defaults` are the paths a source reads by unless a setting states others, and `HostDefaults` the ones a host's folder reads by. |
| `LocalePathBinder` | Builds a file's name from a path for an anchor, `GetName`, in one source and one culture. |
| `LocalePathMatcher` | Reads the anchor a file's name stands for under a path, `TryGetAnchor`, in one source and one culture. |
| `LocalePathValues` | What a path's tokens stand for apart from the anchor: the assembly, the root, the folder of locale files, the culture and the extension. |
| `LocaleConfig` | What an assembly's `.locale.json` says about its files; `Read` takes the runtime's keys and passes over one it does not read. |
| `LocaleConfigException` | Thrown when a config file cannot be read, a key is malformed, or an assembly holds more than one. |

The `Forma.Framework.Localization.Parser` namespace reads the syntax inside a string:
`LocaleExpressionParser` reads the `[$ … $]` expressions in a format and `LocaleFieldParser` the
`name: value;` pairs of an expression body or a metadata entry, and `LocalePathParser` a locale
path's literal runs and tokens, each returning the spans it read and the diagnostics it raised.

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
