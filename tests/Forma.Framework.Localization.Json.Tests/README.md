# Forma.Framework.Localization.Json.Tests

The tests of `Forma.Framework.Localization.Json`: reading a JSON locale file and its metadata,
finding a type's files in the assembly, in locale packs and in the host's folder, and resolving a
lookup across cultures.

```pwsh
./scripts/build/test.ps1 -Project Forma.Framework.Localization.Json.Tests
```

The fixtures are real types and real files, and a test passes only when two names meet: the name
the runtime derives from a type's full name, and the name MSBuild gives an embedded file from its
folder path. Moving a file or renaming a folder therefore changes what is tested. A miss returns
the key itself, so a broken layout shows as an assertion seeing English text; run this project
after moving anything.

## Embedded files

Most types sit in `Fixtures/ResourceSources.cs` in `…Tests.Fixtures`, each with its files in a
folder named for it, so a malformed file troubles no other type.

| Type | Namespace | What it covers | Files |
|---|---|---|---|
| `Colocated` | `…Tests.Fixtures` | The default layout in two cultures, regional fallback to a parent, and the default paths still read beside the ones the config adds | `Fixtures/Colocated/.locales/{he,es}.json` |
| `Unlocalized` | `…Tests.Fixtures` | No file in any culture, so the key comes back | none |
| `GenericArity<T>` | `…Tests.Fixtures` | The `` `1 `` arity dropped from the name | `Fixtures/GenericArity/.locales/he.json` |
| `MainAssembly` | `…Tests.Fixtures.Beside` | A file beside its type, kept in the main assembly | `Fixtures/Beside/MainAssembly.he.json` |
| `InSatellite` | `…Tests.Fixtures.Beside` | A file beside its type, which the build moves into the `he` satellite | `Fixtures/Beside/InSatellite.he.json` |
| `PathScoped` | `…Tests.Fixtures` | A file under a folder of its own, read by a path the config names | `Resources/Fixtures/PathScoped.he.json` |
| `ForeignRoot` | `Contoso.Widgets` | A type whose namespace does not start with the assembly's name | `Fixtures/Foreign/ForeignRoot/.locales/he.json` |
| `GlobalRoot` | none | A type declared without a namespace | `Fixtures/GlobalRoot/.locales/he.json` |
| `Pluralized` | `…Tests.Fixtures` | A localized string carrying a plural expression | `Fixtures/Pluralized/.locales/he.json` |
| `NullValued` | `…Tests.Fixtures` | A `null` value, the source text as the localized string | `Fixtures/NullValued/.locales/{he,fr}.json` |
| `Escaped` | `…Tests.Fixtures` | Keys starting with `$`, one escaped with `$$` and one plain text | `Fixtures/Escaped/.locales/he.json` |
| `Unmarked` | `…Tests.Fixtures` | A file with no metadata entry | `Fixtures/Unmarked/.locales/he.json` |
| `MalformedMetadata` | `…Tests.Fixtures` | A metadata value that is not a field list | `Fixtures/MalformedMetadata/.locales/he.json` |
| `FutureSchema` | `…Tests.Fixtures` | A schema major this build refuses | `Fixtures/FutureSchema/.locales/he.json` |
| `NewerMinor` | `…Tests.Fixtures` | A later minor schema, and a metadata field this build ignores | `Fixtures/NewerMinor/.locales/he.json` |
| `MismatchedCulture` | `…Tests.Fixtures` | A declared culture the file name contradicts | `Fixtures/MismatchedCulture/.locales/he.json` |
| `GroupedFirst`, `GroupedSecond` | `…Tests.Fixtures.Grouped` | A folder's file serving every type in the folder, and a type's own folder winning for the keys it carries | `Fixtures/Grouped/.locales/{he,es}.json`, `Fixtures/Grouped/GroupedSecond/.locales/he.json` |
| `Outer`, `Outer.Inner`, `Outer.InnerGeneric<T>` | `…Tests.Fixtures` | An outer type's file holding its nested types as sections, a nested type's own file, and a nested type's arity dropped | `Fixtures/Outer/.locales/he.json`, `Fixtures/Outer/Inner/.locales/he.json` |
| `OuterGeneric<T>.Inner` | `…Tests.Fixtures` | A generic outer type's arity dropped from its nested type's name | `Fixtures/OuterGeneric/.locales/he.json` |
| `HostDotted` | `…Tests.Fixtures` | An embedded file a host file overrides | `Fixtures/HostDotted/.locales/he.json` |

`.locale.json` at the project root states the three paths the assembly is read by: the default,
a file beside the type it localizes, and a file under `Resources/` read by the type's name. The
library's build embeds it.

The csproj globs embed `Fixtures/` and `Resources/`, and three files need more:

- `MainAssembly.he.json` sets `WithCulture="false"`, so the SDK leaves it in the main assembly
  rather than moving it into a satellite.
- `ForeignRoot`'s file sets `LogicalName` to `Contoso.Widgets.ForeignRoot..locales.he.json`, the
  name the type produces, which its folder path would not.
- `GlobalRoot`'s file sets `LogicalName` for the same reason: a type without a namespace is read
  under the root namespace, which no folder under `Fixtures/` produces.

## A root named by the config

A root namespace named in this assembly's `.locale.json` would move every fixture above, so that
case lives in `Forma.Tests.Support`, whose `.locale.json` names `Forma.Tests.Localization.Rooted`
as the root namespace and `Localization/Rooted/Strings` as the folder the files begin in.

| Type | What it covers | Files |
|---|---|---|
| `RootedWidget` | A root named by `rootNamespace` and `rootDir`, read through the root's `.locales` file and a file named for the assembly | `Localization/Rooted/Strings/.locales/he.json`, `Localization/Rooted/Strings/Forma.Tests.Support.he.json` |
| `RootedToolbar` | A folder beneath the declared root | `Localization/Rooted/Strings/Bars/.locales/he.json` |

## Host files

`.locales/` is copied beside the test assembly, where the default `LocalesPath` looks, so its
layout is a fixture too: a folder named for the assembly, then the type's name as folders, each
folder's files in its own `.locales` folder or directly in it.

| Type | What it covers | Files under `.locales/{assembly}/` |
|---|---|---|
| `HostOverridden` | One key per file overriding the embedded file, from the root's, a parent folder's and the type's own folder's file; a key the assembly lacks; a `null` value; nested sections; and files directly in a folder | `.locales/{he,es}.json`, `es.json`, `Fixtures/.locales/he.json`, `Fixtures/es.json`, `Fixtures/HostOverridden/.locales/he.json` |
| `HostLocalized` | A culture the assembly does not ship, also reached from a regional culture | `Fixtures/HostLocalized/.locales/fr.json` |
| `PathScoped` | Host files following the type's name rather than the folder of its embedded file | `Fixtures/PathScoped/.locales/he.json` |
| `ForeignRoot` | Host files following the full name of a type rooted outside the assembly | `Contoso/Widgets/ForeignRoot/.locales/he.json` |
| `Outer` | A host file for a nested type's outer type | `Fixtures/Outer/.locales/es.json` |
| `HostDotted` | A host file directly in the type's folder overriding one embedded entry and leaving the rest | `Fixtures/HostDotted/he.json` |
| `HostUnmarked` | A host file with no metadata entry | `Fixtures/HostUnmarked/.locales/he.json` |
| `HostMistyped` | A host file giving a key a value that is not text | `Fixtures/HostMistyped/.locales/he.json` |
| `HostNamed` | A host file named for the type, directly in its folder | `Fixtures/HostNamed.he.json` |
| `HostRooted` | A host file in an unrelated folder naming the type from the root with `@.`, found beside files that cannot be read | `Strings/he.json` |

The keys the host files change on `PathScoped` and `ForeignRoot` are ones the embedded-file tests
do not read.

`Hosts/` holds host folders a test names in place of the default. `Hosts/Rooted/` holds the
support assembly's folder, laid out by the root namespace its `.locale.json` declares, so
`RootedWidget` is read from its own folder's file and the root's file, and a file laid out by the
type's full name is ignored.

## Locale packs

`LocalePackTests` builds its packs in memory as `FakeAssembly` instances from `Forma.Tests.Support`,
each carrying a file for the `Fixtures` folder.
