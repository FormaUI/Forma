# Localization test fixtures

The folder layout in this project is not organization — it is the fixture. The runtime reads a
type's files anchor by anchor from its **full name**, and MSBuild names an embedded file from its
**folder path**. A test passes only when those two independently produced names meet. Moving a file
or renaming a folder therefore changes what is under test, and usually breaks it silently: a miss
returns the key itself, so an assertion sees English text rather than an error.

## How a file is placed

The reader lists every resource name the main assembly carries, and those of the culture's
satellite assembly under the names they would carry in the main one, then places each at the part
of a type's name it stands for by the paths in force where it sits. By default that is a folder's
own `.locales` folder, holding one file per culture named for the culture alone, whose sections
complete the name. It walks `CultureInfo.Parent` and stops before the invariant culture, which no
file can name.

## Why each folder exists

**`Fixtures/`** holds the types whose namespace follows their folder, each type's files in a folder
named for it, so a malformed file troubles no other type. The folder name is a namespace segment
(`…Tests.Fixtures`) and therefore appears in every anchor these types produce.

**`Fixtures/Foreign/`** is separate because `ForeignRoot` is deliberately declared in
`Contoso.Widgets`, not in the namespace its folder implies — that mismatch is the scenario. Its
resource carries an explicit `LogicalName`, so the file's location is free.

**`Fixtures/Beside/`** holds files beside the types they localize, the one form whose name ends in
its culture and so the one the build moves into a satellite assembly. **`Resources/`** holds a file
beneath a folder of its own, read by the type's name, so `PathScoped` in `…Tests.Fixtures` has its
file at `Resources/Fixtures/`. Both forms are paths the assembly's one `.locale.json` at the
project root states beside the default, since a config file applies to the whole assembly and
never to one folder.

## The fixtures

| Type | Namespace | What it covers | Resource |
|---|---|---|---|
| `Colocated` | `…Tests.Fixtures` | The default layout, in two cultures, regional fallback to a parent, and that the paths the config adds leave the default read | `Fixtures/Colocated/.locales/{he,es}.json` |
| `Unlocalized` | `…Tests.Fixtures` | No file in any culture, so the key comes back | *(none, deliberately)* |
| `GenericArity<T>` | `…Tests.Fixtures` | The `` `1 `` arity is dropped from the anchor | `Fixtures/GenericArity/.locales/he.json` |
| `MainAssembly` | `…Tests.Fixtures.Beside` | A file beside its type kept out of a satellite | `Fixtures/Beside/MainAssembly.he.json` |
| `InSatellite` | `…Tests.Fixtures.Beside` | A file beside its type, which the build moves into the `he` satellite | `Fixtures/Beside/InSatellite.he.json` |
| `PathScoped` | `…Tests.Fixtures` | A file gathered under a folder of its own, read by a path the assembly's config names | `.locale.json`, `Resources/Fixtures/PathScoped.he.json` |
| `ForeignRoot` | `Contoso.Widgets` | An assembly whose name does not lead its types' root namespace | `Fixtures/Foreign/ForeignRoot/.locales/he.json` |
| `GlobalRoot` | *(none)* | A type declared without a namespace | `Fixtures/GlobalRoot/.locales/he.json` |
| `Pluralized` | `…Tests.Fixtures` | A translation carrying a plural expression | `Fixtures/Pluralized/.locales/he.json` |
| `Unmarked` | `…Tests.Fixtures` | A file carrying no metadata entry | `Fixtures/Unmarked/.locales/he.json` |
| `MalformedMetadata` | `…Tests.Fixtures` | A metadata value that is not a field list | `Fixtures/MalformedMetadata/.locales/he.json` |
| `FutureSchema` | `…Tests.Fixtures` | A schema major this build refuses | `Fixtures/FutureSchema/.locales/he.json` |
| `NewerMinor` | `…Tests.Fixtures` | A later minor schema, and a metadata field this build ignores | `Fixtures/NewerMinor/.locales/he.json` |
| `MismatchedCulture` | `…Tests.Fixtures` | A declared culture the file name contradicts | `Fixtures/MismatchedCulture/.locales/he.json` |
| `GroupedFirst`, `GroupedSecond` | `…Tests.Fixtures.Grouped` | The folder's own file serving every type in the folder, and a type's own folder's file winning over it for the keys it carries | `Fixtures/Grouped/.locales/{he,es}.json`, `Fixtures/Grouped/GroupedSecond/.locales/he.json` |
| `Outer`, `Outer.Inner`, `Outer.InnerGeneric<T>` | `…Tests.Fixtures` | An outer type's file carrying its own strings at the top level and its nested types as sections, a nested type's own file in a folder beneath the outer type's, and a nested type's arity dropped | `Fixtures/Outer/.locales/he.json`, `Fixtures/Outer/Inner/.locales/he.json`, `.locales/{assembly}/Fixtures/Outer/.locales/es.json` |
| `OuterGeneric<T>.Inner` | `…Tests.Fixtures` | A generic outer type's arity dropped from the name its nested type is reached through | `Fixtures/OuterGeneric/.locales/he.json` |
| `HostDotted` | `…Tests.Fixtures` | An embedded file, which a host file directly in the type's folder overrides | `Fixtures/HostDotted/.locales/he.json` |

## Settings embedded beside the files

A `.locale.json` in this assembly naming a root would move every fixture above, so the fixture
proving a root named by an embedded config sits in the `Forma.Tests.Support` assembly instead: its
`.locale.json` at the project root names `Forma.Tests.Localization.Rooted` as the root namespace,
which is not the assembly's name, and `Localization/Rooted/Strings` as the folder its files
begin in.

| Type | What it covers | Files |
|---|---|---|
| `RootedWidget` | A root named by the embedded `.locale.json`, by its `rootNamespace` and a `rootDir` naming a folder beneath the project's root, read through the root's `.locales` file and a file named for the assembly, a form the config adds to the default | `.locale.json`, `Localization/Rooted/Strings/.locales/he.json`, `Localization/Rooted/Strings/Forma.Tests.Support.he.json` |
| `RootedToolbar` | A folder beneath the declared root, read by the root's paths as any folder is | `Localization/Rooted/Strings/Bars/.locales/he.json` |

## The csproj entries

Four files need more than the globs:

- `.locale.json` at the project root is included on its own, since no glob reaches it, and is the
  assembly's one config file, stating the three paths its fixtures are read by.
- `MainAssembly.he.json` sets `WithCulture="false"` so the SDK leaves it in the main assembly
  instead of moving it to a `he` satellite, covering a project that opts out of satellites.
- `ForeignRoot`'s file pins `LogicalName` to `Contoso.Widgets.ForeignRoot..locales.he.json`,
  because the name MSBuild would derive from the folder is not the name the type produces.
- `GlobalRoot`'s file pins `LogicalName` for the same reason: a namespace-less type is anchored
  under the root namespace, which no folder path under `Fixtures/` would produce.

The last three use `Update` rather than `Include`, since `Fixtures\**\*.json` already picks them up.

## The host's files

`.locales/` is copied beside the test assembly, where the factory's default `LocalesPath` looks, so
the layout under it is a fixture too: the first folder is the assembly's root, the rest of the path
is the type's name, and each folder's files sit in its own `.locales` folder or directly in it, the
two forms resolving equally.

| Type | What it covers | Host files |
|---|---|---|
| `HostOverridden` | One key per file, each overriding the embedded file: the root's, a parent folder's and the type's own folder's, a key the assembly never had, a null value, sections nested as objects, and the root's and a folder's file directly in the folder | `.locales/{assembly}/.locales/{he,es}.json`, `.locales/{assembly}/es.json`, `.locales/{assembly}/Fixtures/.locales/he.json`, `.locales/{assembly}/Fixtures/es.json`, `.locales/{assembly}/Fixtures/HostOverridden/.locales/he.json` |
| `HostLocalized` | A culture the assembly does not ship, reached from a regional culture too | `.locales/{assembly}/Fixtures/HostLocalized/.locales/fr.json` |
| `PathScoped` | The host's files following the type's name rather than the folder its embedded file sits in | `.locales/{assembly}/Fixtures/PathScoped/.locales/he.json` |
| `ForeignRoot` | The host's files following the full name of a type rooted outside the assembly | `.locales/{assembly}/Contoso/Widgets/ForeignRoot/.locales/he.json` |
| `HostDotted` | A host file directly in the type's folder overriding an embedded entry, and leaving the ones it does not name | `.locales/{assembly}/Fixtures/HostDotted/he.json` |
| `HostUnmarked` | A host file carrying no metadata entry | `.locales/{assembly}/Fixtures/HostUnmarked/.locales/he.json` |
| `HostMistyped` | A host file giving a key a value that is not text | `.locales/{assembly}/Fixtures/HostMistyped/.locales/he.json` |
| `HostNamed` | A host file named for the type, directly in its folder | `.locales/{assembly}/Fixtures/HostNamed.he.json` |
| `HostRooted` | A host file in an unrelated folder naming the type from the root with `@.`, found although the culture holds files that cannot be read | `.locales/{assembly}/Strings/he.json` |

The keys the host files change on `PathScoped` and `ForeignRoot` are not the ones the other tests
read, so those tests see the embedded translation as before.

`Hosts/` holds host folders a test names in place of the default one. `Hosts/Rooted/` holds the
support assembly's folder, laid out by the root namespace that assembly's own `.locale.json`
declares and holding no config of its own, so `RootedWidget` is read from its own folder's file and
through the sections of the root's file, and a file laid out by the type's full name is ignored.

## Before you move anything

Run this project's tests. They are the only check that the layout still matches the derivation, and they
exist because of a real defect — a locale resolving against the assembly name where it should have
used the type's own root returned English for every foreign-rooted type.
