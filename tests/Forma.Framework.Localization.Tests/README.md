# Localization test fixtures

The folder layout in this project is not organisation — it is the fixture. `JsonStringLocalizerFactory`
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

The reader then looks for `{baseName}.json` inside the satellite assembly for the current UI culture,
falling back to `{baseName}.{culture}.json` in the main assembly, walking `CultureInfo.Parent` until the
neutral culture is reached.

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
| `NeutralBacked` | `…Tests.Fixtures` | A culture file backed by a neutral one for the keys it omits | `Fixtures/NeutralBacked.he.json`, `NeutralBacked.json` |
| `MainAssembly` | `…Tests.Fixtures` | A culture file kept out of a satellite | `Fixtures/MainAssembly.he.json` |
| `PathScoped` | `…Tests.Fixtures` | `ResourcesPath` inserting a segment, with the namespace tail mirrored beneath it | `Resources/Fixtures/PathScoped.he.json` |
| `ForeignRoot` | `Contoso.Widgets` | An assembly whose name does not lead its types' root namespace | `Fixtures/Foreign/ForeignRoot.he.json` |
| `GlobalRoot` | *(none)* | A type declared without a namespace | `Fixtures/GlobalRoot.he.json` |

## The csproj entries

Three files need more than the two globs:

- `MainAssembly.he.json` sets `WithCulture="false"` so the SDK leaves it in the main assembly instead of
  moving it to a `he` satellite, covering a project that opts out of satellites.
- `ForeignRoot.he.json` pins `LogicalName` to `Contoso.Widgets.ForeignRoot.he.json`, because the name
  MSBuild would derive from the folder is not the name the type produces.
- `GlobalRoot.he.json` pins `LogicalName` for the same reason: a namespace-less type yields
  `{assembly}.GlobalRoot`, which no folder path under `Fixtures/` would produce.

The last two use `Update` rather than `Include`, since `Fixtures\**\*.json` already picks them up.

## Before you move anything

Run this project's tests. They are the only check that the layout still matches the derivation, and they
exist because of a real defect — a localizer resolving against the assembly name where it should have
used the type's own root returned English for every foreign-rooted type.
