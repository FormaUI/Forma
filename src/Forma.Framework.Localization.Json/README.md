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
`Forma.Framework.Localization.Abstractions` documents.

Put a file beside the type it translates and mark it `EmbeddedResource`:

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

A locale resolves a **base name** from the type's full name, then looks for a file under it:

| Type's full name | Base name |
| --- | --- |
| Starts with the assembly name | `{assembly}.[{ResourcesPath}.]{remainder}` |
| Contains a dot but a different root | `{full name}` |
| No dot at all | `{assembly}.[{ResourcesPath}.]{name}` |

Generic arity and nested-type markers are dropped: `Grid<T>` reads `Grid.he.json`, `Outer+Inner` reads
`Outer.Inner.he.json`, and a closed generic resolves through its open definition.

Lookup walks `CultureInfo.CurrentUICulture` outward through `Parent`, taking the first file that has
the key and stopping before the invariant culture, which no file can name. Each culture is tried in
the satellite assembly first, then as `{baseName}.{culture}.json` in the main assembly. Files are read
once and cached frozen.

`ResourcesPath` inserts a folder segment between the assembly name and the remainder. It resolves from
the assembly root, and a type rooted outside the assembly bypasses it.

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
accepted in either direction, and **patch** is ignored. A field this build does not recognise is
ignored, so a file written by a newer toolchain still loads.

The linter enforces the marker's position, the required fields and their order, and that a file
declares the current schema.

## Plural rules

A translation resolves its plural expressions before the holes are filled.
`Forma.Framework.Localization.Abstractions` documents the syntax.

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
