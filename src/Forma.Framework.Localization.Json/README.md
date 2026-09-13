# Forma.Framework.Localization.Json

The JSON-backed localization backend: a component's translations live in JSON files that sit beside
the component and ship inside its assembly, and a lookup resolves the right one from the type asking
and the culture in force.

## Getting started

Register the backend:

```csharp
services.AddJsonLocalization();
services.AddJsonLocalization(new JsonLocalizationOptions { ResourcesPath = "Resources", SourceLanguage = "es" });
```

That registers `IStringLocale<T>` and `IStringLocaleFactory`, so a consumer injecting either
resolves. `JsonLocalizationOptions` adds `ResourcesPath` to the settings every backend shares, which
`Forma.Framework.Localization` documents.

Put a file beside the type it translates and mark it `EmbeddedResource` with `WithCulture="false"`, so
it stays in the assembly itself rather than moving to a satellite assembly:

```text
FormaButton.razor.cs
FormaButton.he.json
FormaButton.es.json
```

Inject the locale for that type and look a key up by its English source text:

```csharp
[Inject] private IStringLocale<FormaButton> L { get; set; } = default!;

// renders "שמור" under he, "Guardar" under es, "Save" under en
L["Save"]
```

There is no file for the source language. A key with no translation falls back to itself, and the
key *is* the source text.

## Translation files

A file is a flat object keyed by the English source text:

```json
{
    "Save": "שמור",
    "Stored {stored} of {total}.": "נשמרו {stored} מתוך {total}."
}
```

Every file names a culture in its own file name and in its [metadata](#file-metadata); there is no
cultureless file.

## Untranslated messages

A `null` value means that the source message has been deliberately left untranslated. This lets a
localization file explicitly account for a message without repeating it as its own translation:

```json
{
    "Email": null,
    "PDF": null,
    "Forma": null
}
```

Use `null` where the right translation for this culture is the source text itself: a term the language
borrows, an acronym, a brand or proper noun, a symbol. Leave the key absent while its translation is
still owed.

A value on its own is a different matter, and does not belong in a translation file at all. There is
nothing to look up in `{count}` or `[$ plural; other: #; $]`, and `$"{count}"` or
`count.ToString("N0", CultureInfo.CurrentCulture)` renders each of them identically without a key, an
entry, or a culture file to keep in step. Route text through the localizer and format values directly.
The linter reports LOC0211 and LOC0212 for a call site written this way, whether or not the file
carries a null entry for it.

A null entry is not the same as an absent entry. In both cases there is no translated message, so the
source message is used instead. The difference is that a null entry explicitly declares that this is
intentional, while an absent entry means that a translation is still missing.

| | `"Email": null` | key absent |
| --- | --- | --- |
| `ResourceNotFound` | `false` | `true` |
| `RequireMatchingLocalization` | passes | throws |
| `GetAllStrings` | lists it | omits it |

Using the source message does not mean emitting it literally. The source is still interpreted normally:
holes are filled and plural expressions are evaluated, so a null `"Stored {stored} of {total}."` still
renders both its values. Because no translated message exists, plural rules are taken from
`SourceLanguage` rather than the current culture.

The `null` itself is only a declaration that no translation is required; downstream formatting and
rendering operate on the source message and never consume a null value.

## How a file is found

A locale resolves a **base name** from the type's full name, which every `LocalizedText` reports as
the location it searched:

| Type's full name | Base name |
| --- | --- |
| Starts with the assembly name | `{assembly}.[{ResourcesPath}.]{remainder}` |
| Contains a dot but a different root | `{full name}` |
| No dot at all | `{assembly}.[{ResourcesPath}.]{name}` |

Generic arity and nested-type markers are dropped: `Grid<T>` reads `Grid.he.json`, `Outer+Inner` reads
`Outer.Inner.he.json`, and a closed generic resolves through its open definition.

A type's translations sit in the file named for the type, or in a file named for a namespace above
it, where objects complete the rest of the name. Walking down from the assembly, each of these
translates `FormaToolbar` in `Forma.Blazor.Components.Bars`:

```text
Forma.Blazor.es.json                    { "Components.Bars.FormaToolbar": { "Bold": "Negrita" } }
Components/Bars.es.json                 { "FormaToolbar": { "Bold": "Negrita" } }
Components/Bars/Bars.es.json            { "FormaToolbar": { "Bold": "Negrita" } }
Components/Bars/FormaToolbar.es.json    { "Bold": "Negrita" }
```

A file named for a folder sits beside that folder or inside it under the folder's own name, and the
two are one and the same; the assembly's file sits at the project root under the assembly's name. An
object's key may spell several segments at once or one at a time, so
`"Components": { "Bars.FormaToolbar": { … } }` reads the same as the first line. A file holds only
the keys it changes: a file closer to the type wins over one higher up, and the type's own file over
every other.

Lookup walks `CultureInfo.CurrentUICulture` outward through `Parent`, taking the first culture whose
files have the key and stopping before the invariant culture, which no file can name. Within a
culture, a file is read from the main assembly first, then from that culture's satellite assembly.
Files are read once and cached frozen.

`ResourcesPath` inserts a folder segment between the assembly name and the remainder. It resolves from
the assembly root, and a type rooted outside the assembly bypasses it.

## Translations from the host

A host lays its own files in a `locales` folder beside the application, to reword a string an
assembly ships, or to add a language it never had. `LocalesPath` names the folder, resolved
against the application's base directory when relative; an empty value reads none.

The folder is laid out as an assembly's own files are, under a folder named for the assembly, so
each of these translates `FormaComposer` in `Forma.Blazor.Components.Composer`:

```text
locales/Forma.Blazor.es.json                                     { "Components.Composer.FormaComposer": { "Bold": "Negrita" } }
locales/Forma.Blazor/Components.es.json                          { "Composer.FormaComposer": { "Bold": "Negrita" } }
locales/Forma.Blazor/Components/Composer/FormaComposer.es.json   { "Bold": "Negrita" }
```

The same precedence applies, and a host's entry wins over the assembly's own at the same position,
so a host file holds only the keys it changes and every other key keeps the assembly's translation.

Host overrides respect culture specificity. Lookup takes a key from the most specific culture that
has it, from either source, before falling back to the parent, so an override written in `es` is a
fallback for an `es-419` reader rather than a replacement for every Spanish string; an override of a
key belongs in the culture that supplies that key.

A host file completes a type's name from the assembly's name, without `ResourcesPath`, so the
host's layout follows the namespace rather than the embedding. An assembly whose root namespace is
not its name has its host files declare the root in their metadata, as
`RootNamespace: Forma.Framework.Resources.Office;`, and the path and sections then complete the
name from there: a file declaring that root at
`locales/Forma.AddOns.Resources.Office/OfficeResourceProvider.es.json` translates
`Forma.Framework.Resources.Office.OfficeResourceProvider`. A file declaring no root, for a type whose
namespace does not start with the assembly's name, is laid out by the type's full name. Files under a
declared root are read after the assembly's own, so their entries win. An embedded file needs no
root, since its name already spells one. Every file carries the [metadata](#file-metadata) entry,
declaring the culture its name gives it.

The folder has to reach the application's output: a web project copies JSON content there on its
own, and any other project marks the folder as content copied to the output directory.

## File metadata

Every localization file declares what it is, as its first property:

```json
{
  "$localization": "SchemaVersion: 1.0.0; Culture: he;",
  "Save": "שמור"
}
```

The entry is removed before lookup, so it appears in neither a key search nor `GetAllStrings`.

Reading a file without it throws `LocalizationMetadataException`, as does a malformed value, a
`Culture` other than the one the file's own name gives it, or a `SchemaVersion` this build cannot
read.

`SchemaVersion` is compared by part: a differing **major** is refused, a differing **minor** is
accepted in either direction, and **patch** is ignored. A field this build does not recognize is
ignored, so a file written by a newer toolchain still loads.

The linter enforces the marker's position, the required fields and their order, and that a file
declares the current schema.

## Plural rules

A translation resolves its plural expressions before the holes are filled.
`Forma.Framework.Localization` documents the syntax.

```json
"{selected} selected": "[$ [selected] plural; one: נבחר אחד; two: שניים נבחרו; other: # נבחרו; $]"
```

An expression may sit in the translation alone, so a key carries one only where the source language
itself inflects.

Rules read CLDR operands rather than a bare integer, so `1` and `1.0` can differ: `1.0` is `one` in
Spanish and `other` in English.

`SourceLanguage` names the language the keys are written in, and defaults to `en`. An untranslated key
resolves its plural forms under that language — a Hebrew key keeps its dual in front of an English
reader — while the count itself follows the reader, so an English key shows `1.234,5` to a Colombian.
