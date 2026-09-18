# Forma.Framework.Localization.Json

The JSON backend for `Forma.Framework.Localization`: it implements `IStringLocaleFactory` and
`IStringLocale<T>` over JSON locale files that sit in a `.locales` folder beside the types they
localize and ship inside their assembly, with the host's own files laid over them. A lookup resolves the right file
from the type asking and the culture in force, and a key with no translation renders as written.

## Install and register

```pwsh
dotnet add package 2Site.Forma.Framework.Localization.Json
```

```csharp
using Forma.Framework.Localization;
using Forma.Framework.Localization.Json;

builder.Services.AddJsonLocalization();

builder.Services.AddJsonLocalization(new LocalizationOptions
{
    RequireMatchingLocalization = builder.Environment.IsDevelopment(),
});
```

That registers `IStringLocaleFactory` and `IStringLocale<T>` as singletons, so a consumer
injecting either resolves. Registration uses `TryAdd`, so a host that registered either contract
first keeps its own. The locales log through the host's `ILoggerFactory`, and discard their
messages where none is registered. The options are the `LocalizationOptions` every backend shares,
which `Forma.Framework.Localization` documents.

| Type | What it is |
| --- | --- |
| `ServiceCollectionExtensions` | `AddJsonLocalization`, with or without options. |
| `JsonStringLocaleFactory` | Creates locales reading JSON files embedded in the assembly owning the type, with the host's files laid over them; constructed with no arguments, with options, or with options and an `ILoggerFactory`. |
| `JsonStringLocale<T>` | The `IStringLocale<T>` the container resolves, reading the strings belonging to `T`. |
| `JsonLocaleFileFormat` | The `ILocaleFileFormat` a JSON locale file is written in, for a tool reading files by the runtime's rule. |

## Getting started

Put a file for each culture in the `.locales` folder beside the type it localizes, named for the
culture, and embed it:

```text
Uploads/UploadStatus.cs
Uploads/.locales/es.json
Uploads/.locales/he.json
```

```xml
<ItemGroup>
    <EmbeddedResource Include="**\*.json" Exclude="$(DefaultItemExcludes)" />
</ItemGroup>
```

The SDK embeds no JSON by default, so the item names the files. A file named for its culture alone
stays in the assembly itself rather than moving into a satellite assembly.

A file is a JSON object whose sections complete the type's name from its folder, keyed by the
source text and opening with the metadata entry:

```json
{
  "$localization": "SchemaVersion: 1.0.0; Culture: es;",
  "UploadStatus": {
    "Saved": "Guardado",
    "Stored {Stored} of {Total}.": "Se guardaron {Stored} de {Total}.",
    "[$ plural; one: # file; other: # files; $]": "[$ plural; one: # archivo; other: # archivos; $]"
  }
}
```

Inject the locale for the type and look a key up by the source text:

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

Under `es`, `Progress(3, 9)` renders `Se guardaron 3 de 9.` and `FileCount(1)` renders
`1 archivo`; under `en`, with no file, they render `Stored 3 of 9.` and `1 file` from the keys
themselves. A Razor component injects the same locale, `@inject IStringLocale<UploadPage> L`.
Values pair with the key's holes by position, and `Forma.Framework.Localization` documents the
hole and plural syntax in full.

## Locale files

Every file names its culture in its own file name and in its metadata; there is no cultureless
file, and the source language needs none. A text value is a translation, a nested object is a
section completing a type's name, and `null` says the translation is deliberately the source text
itself. Comments and trailing commas are allowed.

`Forma.Framework.Localization` owns the layout: a type's translations are read from the root down
to the type, each folder's from the `.locales` folder inside it unless the assembly's `.locale.json`
states other paths, embedded in the assembly or laid in the host's `.locales` folder. It also
defines the `$shared` section any node may hold and the `$$` escape
for a key shaped like a reserved entry. This backend adds the format alone: the extension is
`.json`, and the metadata entry is the `$localization` property.

### Metadata

```json
{
  "$localization": "SchemaVersion: 1.0.0; Culture: he;",
  "Save": "שמור"
}
```

The value is a list of `Name: value;` fields. `SchemaVersion` and `Culture` are required, and a
file may add `Author` and `Revision`. Where a file belongs is the business of the assembly's `.locale.json`,
never of the entry. The entry is removed before lookup, so it
appears in neither a key search nor `GetAllStrings`.

Reading a file without the entry throws `LocalizationMetadataException`, as does a malformed
value, a `Culture` other than the one the file's own name gives it, or a `SchemaVersion` this
build cannot read. The version is compared by part: a differing **major** is refused, a differing
**minor** is accepted in either direction, and **patch** is ignored. A field this build does not
recognize is ignored, so a file written by a newer minor schema still loads.

### Source-text entries

A `null` value declares that the translation for this culture is the source text itself, for a
term the language borrows, an acronym, a brand or proper noun, or a symbol:

```json
{
  "$localization": "SchemaVersion: 1.0.0; Culture: he;",
  "Email": null,
  "PDF": null,
  "Forma": null
}
```

A null entry and an absent entry both render the source text, and differ in what they report:

| | `"Email": null` | key absent |
| --- | --- | --- |
| `ResourceNotFound` | `false` | `true` |
| `RequireMatchingLocalization` | passes | throws |
| `GetAllStrings` | lists it | omits it |

The source text is still interpreted: holes are filled and plural expressions evaluated, with the
plural rules of `SourceLanguage` rather than the culture on screen.

## How a lookup resolves

A lookup walks `CultureInfo.CurrentUICulture` outward through `Parent`, taking the first culture
whose files carry the key and stopping before the invariant culture, which no file can name. So an
`es-419` reader gets the `es` file's entry where no `es-419` file has it, and a host override
written in `es` is a fallback for that reader rather than a replacement for every Spanish string.

Within a culture, the assembly's embedded files are read first and the host's `.locales` folder is
laid over them, so a host entry wins over the assembly's own at the same position. Each file is
read once per factory and kept in memory.

A plural expression in a translation is resolved before the holes are filled, and the rules read
CLDR operands rather than a bare integer, so `1` and `1.0` can differ: `1.0` is `one` in Spanish
and `other` in English. A key with no translation resolves its plural forms under
`SourceLanguage`, `en` by default, while the count itself follows the reader, so an English key
shows `1.234,5` to a Colombian.

## Outside the container

A factory is constructed directly where no container is at hand, and creates a locale for a type
or for a base name inside a named assembly:

```csharp
using Forma.Framework.Localization;
using Forma.Framework.Localization.Json;

var factory = new JsonStringLocaleFactory(new LocalizationOptions { LocalesPath = string.Empty });

IStringLocale locale = factory.Create(typeof(UploadStatus));
IStringLocale shared = factory.Create("Forma.Blazor.Components.Bars", "Forma.Blazor");
```

A factory returns the same locale for the same type, and every locale reads the culture in force
on each lookup.

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
