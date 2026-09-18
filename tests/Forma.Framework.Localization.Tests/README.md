# Localization test fixtures

The folder layout in this project is not organization — it is the fixture. `JsonStringLocaleFactory`
derives a resource's base name from the **type's full name**, and MSBuild derives an embedded resource's
manifest name from the **file's folder path**. A test passes only when those two independently produced
strings meet. Moving a file or renaming a folder therefore changes what is under test, and usually
breaks it silently: a miss returns the key itself, so an assertion sees English text rather than an error.

## How a base name is derived

`GetBaseName` picks one of three branches, and only the first two folder layouts follow from it:

| Type's full name | Branch taken | Base name |
|---|---|---|
| Starts with the assembly name | strip that prefix, then qualify | `{assembly}.{remainder}` |
| Contains a dot but a different root | used verbatim | `{full name}` |
| No dot at all | qualify the bare name | `{assembly}.{name}` |

The reader lists every resource name the main assembly carries, and those of the culture's
satellite assembly under the names they would carry in the main one, then places each at the part
of a type's name it stands for by the default paths: a file named for the type, or for a namespace
above it, beside its folder, inside it under the folder's own name, or in a `.locales` folder, whose
sections complete the name. It walks `CultureInfo.Parent` and stops before the invariant culture,
which no file can name.

## Why each folder exists

**`Fixtures/`** holds the types whose namespace follows their folder, plus the files that sit beside them.
The folder name is a namespace segment (`…Tests.Fixtures`) and therefore appears in every base name these
types produce. Renaming it renames the namespace, and every colocated `.json` follows automatically.

**`Fixtures/Foreign/`** is separate because `ForeignRoot` is deliberately declared in `Contoso.Widgets`,
not in the namespace its folder implies — that mismatch is the scenario. Keeping it beside types whose
namespace *does* track the folder would suggest the folder governs the namespace here, and it does not.
Its resource carries an explicit `LogicalName`, so the file's location is free.

**`Resources/`** holds its own `.locali.json`, whose one path reads a file beneath the folder by the
type's name, so `PathScoped` in `…Tests.Fixtures` has its file at `Resources/Fixtures/`. The empty
`.locali.json` at the project root marks where the assembly's names begin, which keeps the one under
`Resources/` a nested settings file, in force beneath its folder alone, rather than the root.

## The fixtures

| Type | Namespace | What it covers | Resource |
|---|---|---|---|
| `Colocated` | `…Tests.Fixtures` | The default layout, in two cultures, regional fallback to a parent, and that another folder's settings leave it read | `Fixtures/Colocated.{he,es}.json` |
| `Unlocalized` | `…Tests.Fixtures` | No file in any culture, so the key comes back | *(none, deliberately)* |
| `GenericArity<T>` | `…Tests.Fixtures` | The `` `1 `` arity is dropped from the file name | `Fixtures/GenericArity.he.json` |
| `MainAssembly` | `…Tests.Fixtures` | A culture file kept out of a satellite | `Fixtures/MainAssembly.he.json` |
| `PathScoped` | `…Tests.Fixtures` | A folder whose own settings file names the path its files are read by | `Resources/.locali.json`, `Resources/Fixtures/PathScoped.he.json` |
| `ForeignRoot` | `Contoso.Widgets` | An assembly whose name does not lead its types' root namespace | `Fixtures/Foreign/ForeignRoot.he.json` |
| `GlobalRoot` | *(none)* | A type declared without a namespace | `Fixtures/GlobalRoot.he.json` |
| `Pluralized` | `…Tests.Fixtures` | A translation carrying a plural expression | `Fixtures/Pluralized.he.json` |
| `Unmarked` | `…Tests.Fixtures` | A file carrying no metadata entry | `Fixtures/Unmarked.he.json` |
| `MalformedMetadata` | `…Tests.Fixtures` | A metadata value that is not a field list | `Fixtures/MalformedMetadata.he.json` |
| `FutureSchema` | `…Tests.Fixtures` | A schema major this build refuses | `Fixtures/FutureSchema.he.json` |
| `NewerMinor` | `…Tests.Fixtures` | A later minor schema, and a metadata field this build ignores | `Fixtures/NewerMinor.he.json` |
| `MismatchedCulture` | `…Tests.Fixtures` | A declared culture the file name contradicts | `Fixtures/MismatchedCulture.he.json` |
| `GroupedFirst`, `GroupedSecond` | `…Tests.Fixtures.Grouped` | The file named for the folder, read from inside it and from beside it, serving every type in the folder; a type's own file winning over it for the keys it carries | `Fixtures/Grouped/Grouped.he.json`, `Fixtures/Grouped.es.json`, `Fixtures/Grouped/GroupedSecond.he.json` |
| `Outer`, `Outer.Inner`, `Outer.InnerGeneric<T>` | `…Tests.Fixtures` | An outer type's file carrying its own strings at the top level and its nested types as sections, a nested type's own file laid out as a folder path, and a nested type's arity dropped | `Fixtures/Outer.he.json`, `Fixtures/Outer/Inner.he.json`, `locales/{assembly}/Fixtures/Outer.es.json` |
| `OuterGeneric<T>.Inner` | `…Tests.Fixtures` | A generic outer type's arity dropped from the name its nested type is reached through | `Fixtures/OuterGeneric.he.json` |
| `DottedWidget` | `…Tests.Fixtures.Locales` | Every `.locales` form a default path reads: the folder's file inside its `.locales` folder, the culture's file there winning over it, and the type's file in its parent's `.locales` folder winning over both | `Fixtures/Locales/.locales/Locales.he.json`, `Fixtures/Locales/.locales/he.json`, `Fixtures/Locales/.locales/DottedWidget.he.json` |
| `HostDotted` | `…Tests.Fixtures` | A file beside the type, which a host file in a form the assembly does not use overrides | `Fixtures/HostDotted.he.json` |

The `.locales` files named for a type or a folder travel in the `he` satellite, and `.locales/he.json`,
whose culture is its whole name, stays in the main assembly, so the one fixture covers both.

## Settings embedded beside the files

A `.locali.json` in this assembly naming a root would move every fixture above, so the
fixture proving a root named by embedded settings sits in the `Forma.Tests.Support` assembly
instead, under `Localization/Rooted/`. That assembly's root namespace, `Forma.Tests`, is not its
name, which is the case the settings exist for.

| Type | What it covers | Files |
|---|---|---|
| `RootedWidget` | A root named by an embedded `.locali.json`, by its `rootNamespace` and a `rootDir` naming a folder, read through a `.locales` file at the root and the file named for an assembly whose root namespace differs | `Rooted/.locali.json`, `Rooted/Strings/.locales/he.json`, `Rooted/Strings/Forma.Tests.Support.he.json` |
| `RootedToolbar` | A nested `.locali.json` changing the paths and the folder name beneath it, so a default form there is no longer read | `Rooted/Strings/Bars/.locali.json`, `Rooted/Strings/Bars/i18n/Bars.he.json`, `Rooted/Strings/Bars/RootedToolbar.he.json` |

## The csproj entries

Four files need more than the globs:

- `.locali.json` at the project root is included on its own, since no glob reaches it, and marks
  where the assembly's names begin.
- `MainAssembly.he.json` sets `WithCulture="false"` so the SDK leaves it in the main assembly instead of
  moving it to a `he` satellite, covering a project that opts out of satellites.
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
| `HostLocalized` | A culture the assembly does not ship, reached from a regional culture too | `locales/{assembly}/Fixtures/HostLocalized.fr.json` |
| `PathScoped` | The host's layout following the type's name rather than the folder its embedded file sits in | `locales/{assembly}/Fixtures/PathScoped.he.json` |
| `ForeignRoot` | The host's layout following the full name of a type rooted outside the assembly | `locales/{assembly}/Contoso/Widgets/ForeignRoot.he.json` |
| `HostDotted` | A host file in a `.locales` folder overriding an embedded entry from a form the assembly does not use | `locales/{assembly}/Fixtures/.locales/HostDotted.he.json` |
| `HostUnmarked` | A host file carrying no metadata entry | `locales/{assembly}/Fixtures/HostUnmarked.he.json` |
| `HostMistyped` | A host file giving a key a value that is not text | `locales/{assembly}/Fixtures/HostMistyped.he.json` |

The keys the host files change on `PathScoped` and `ForeignRoot` are not the ones the other tests
read, so those tests see the embedded translation as before.

`Hosts/` holds host folders a test names in place of the default one, since a root named in the
default folder would move every other fixture there. `Hosts/Rooted/` names `Contoso.Widgets` as the
root in its assembly folder's `.locali.json`, so `ForeignRoot` is read from the file named for the
type and through the sections of the file named for the assembly, and a file whose path does not
meet that root is ignored.

## Before you move anything

Run this project's tests. They are the only check that the layout still matches the derivation, and they
exist because of a real defect — a locale resolving against the assembly name where it should have
used the type's own root returned English for every foreign-rooted type.
