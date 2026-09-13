# Localization test fixtures

The folder layout in this project is not organization — it is the fixture. `JsonStringLocaleFactory`
derives a resource's base name from the **type's full name**, and MSBuild derives an embedded resource's
manifest name from the **file's folder path**. A test passes only when those two independently produced
strings meet. Moving a file or renaming a folder therefore changes what is under test, and usually
breaks it silently: a miss returns the key itself, so an assertion sees English text rather than an error.

## How a base name is derived

`BaseNameFor` picks one of three branches, and only the first two folder layouts follow from it:

| Type's full name | Branch taken | Base name |
|---|---|---|
| Starts with the assembly name | strip that prefix, then qualify | `{assembly}.[{ResourcesPath}.]{remainder}` |
| Contains a dot but a different root | used verbatim | `{full name}` |
| No dot at all | qualify the bare name | `{assembly}.[{ResourcesPath}.]{name}` |

`ResourcesPath` is inserted between the assembly name and the remainder, so it only affects the first
and third branches. A type rooted outside the assembly bypasses it entirely.

The reader then looks for `{baseName}.{culture}.json` in the main assembly, falling back to
`{baseName}.json` inside the satellite assembly for that culture, and for each file named for a
namespace above the type, beside its folder or inside it under the folder's own name, whose sections
complete the name; it walks `CultureInfo.Parent` and stops before the invariant culture, which no
file can name.

## Why each folder exists

**`Fixtures/`** holds the types whose namespace follows their folder, plus the files that sit beside them.
The folder name is a namespace segment (`…Tests.Fixtures`) and therefore appears in every base name these
types produce. Renaming it renames the namespace, and every colocated `.json` follows automatically.

**`Fixtures/Foreign/`** is separate because `ForeignRoot` is deliberately declared in `Contoso.Widgets`,
not in the namespace its folder implies — that mismatch is the scenario. Keeping it beside types whose
namespace *does* track the folder would suggest the folder governs the namespace here, and it does not.
Its resource carries an explicit `LogicalName`, so the file's location is free.

**`Resources/Fixtures/`** cannot be nested under `Fixtures/`. `ResourcesPath = "Resources"` resolves from
the assembly root, so the folder has to be top level; and because `PathScoped` lives in `…Tests.Fixtures`,
its file must sit at `Resources/Fixtures/` for the derived manifest name to match. That mirroring is
precisely what the test asserts, so the duplicated `Fixtures` segment is required, not redundant.

## The fixtures

| Type | Namespace | What it covers | Resource |
|---|---|---|---|
| `Colocated` | `…Tests.Fixtures` | The default layout, in two cultures, regional fallback to a parent, and that it stops being found once `ResourcesPath` is set | `Fixtures/Colocated.{he,es}.json` |
| `Untranslated` | `…Tests.Fixtures` | No file in any culture, so the key comes back | *(none, deliberately)* |
| `GenericArity<T>` | `…Tests.Fixtures` | The `` `1 `` arity is dropped from the file name | `Fixtures/GenericArity.he.json` |
| `MainAssembly` | `…Tests.Fixtures` | A culture file kept out of a satellite | `Fixtures/MainAssembly.he.json` |
| `PathScoped` | `…Tests.Fixtures` | `ResourcesPath` inserting a segment, with the namespace tail mirrored beneath it | `Resources/Fixtures/PathScoped.he.json` |
| `ForeignRoot` | `Contoso.Widgets` | An assembly whose name does not lead its types' root namespace | `Fixtures/Foreign/ForeignRoot.he.json` |
| `GlobalRoot` | *(none)* | A type declared without a namespace | `Fixtures/GlobalRoot.he.json` |
| `Pluralized` | `…Tests.Fixtures` | A translation carrying a plural expression | `Fixtures/Pluralized.he.json` |
| `Unmarked` | `…Tests.Fixtures` | A file carrying no metadata entry | `Fixtures/Unmarked.he.json` |
| `MalformedMetadata` | `…Tests.Fixtures` | A metadata value that is not a field list | `Fixtures/MalformedMetadata.he.json` |
| `FutureSchema` | `…Tests.Fixtures` | A schema major this build refuses | `Fixtures/FutureSchema.he.json` |
| `NewerMinor` | `…Tests.Fixtures` | A later minor schema, and a metadata field this build ignores | `Fixtures/NewerMinor.he.json` |
| `MismatchedCulture` | `…Tests.Fixtures` | A declared culture the file name contradicts | `Fixtures/MismatchedCulture.he.json` |
| `GroupedFirst`, `GroupedSecond` | `…Tests.Fixtures.Grouped` | The file named for the folder, read from inside it and from beside it, serving every type in the folder; a type's own file winning over it for the keys it carries | `Fixtures/Grouped/Grouped.he.json`, `Fixtures/Grouped.es.json`, `Fixtures/Grouped/GroupedSecond.he.json` |

## The csproj entries

Four files need more than the two globs:

- `MainAssembly.he.json` sets `WithCulture="false"` so the SDK leaves it in the main assembly instead of
  moving it to a `he` satellite, covering a project that opts out of satellites.
- `Grouped.es.json` sets `WithCulture="false"` for the same reason, so the file named for a folder is read from
  the main assembly as well as from a satellite.
- `ForeignRoot.he.json` pins `LogicalName` to `Contoso.Widgets.ForeignRoot.he.json`, because the name
  MSBuild would derive from the folder is not the name the type produces.
- `GlobalRoot.he.json` pins `LogicalName` for the same reason: a namespace-less type yields
  `{assembly}.GlobalRoot`, which no folder path under `Fixtures/` would produce.

The last two use `Update` rather than `Include`, since `Fixtures\**\*.json` already picks them up.

## The host's files

`locales/` is copied beside the test assembly, where the factory's default `LocalesPath` looks, so
the layout under it is a fixture too: the first folder is the assembly's name, the rest of the path
is the type's name, and a file higher up completes the name with sections inside it.

| Type | What it covers | Host files |
|---|---|---|
| `HostOverridden` | One key per file shape, each overriding the file beside the type, a key the assembly never had, a null value, and sections nested as objects | `locales/{assembly}.he.json`, `locales/{assembly}.es.json`, `locales/{assembly}/Fixtures.he.json`, `locales/{assembly}/Fixtures/HostOverridden.he.json` |
| `HostTranslated` | A culture the assembly does not ship, reached from a regional culture too | `locales/{assembly}/Fixtures/HostTranslated.fr.json` |
| `PathScoped` | The host's layout following the type's name rather than `ResourcesPath` | `locales/{assembly}/Fixtures/PathScoped.he.json` |
| `ForeignRoot` | The host's layout following the full name of a type rooted outside the assembly, and the layout under a root a file declares, from the file named for the type and through sections; a file whose declared root does not meet its path is ignored | `locales/{assembly}/Contoso/Widgets/ForeignRoot.he.json`, `locales/{assembly}/ForeignRoot.es.json`, `locales/{assembly}/Widgets.es.json`, `locales/{assembly}/Widgets/ForeignRoot.es.json` |
| `HostUnmarked` | A host file carrying no metadata entry | `locales/{assembly}/Fixtures/HostUnmarked.he.json` |
| `HostMistyped` | A host file giving a key a value that is not text | `locales/{assembly}/Fixtures/HostMistyped.he.json` |

The keys the host files change on `PathScoped` and `ForeignRoot` are not the ones the other tests
read, so those tests see the embedded translation as before.

## Before you move anything

Run this project's tests. They are the only check that the layout still matches the derivation, and they
exist because of a real defect — a locale resolving against the assembly name where it should have
used the type's own root returned English for every foreign-rooted type.
