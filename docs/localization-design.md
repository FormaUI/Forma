# Forma Localization Design

## Scope

How locale files are laid out and found. Authoritative for the paths a source reads its files by,
the tokens a path is written in, the `.locali.json` settings that shape it, and the order entries
are applied in. The contract a component reads strings through, holes, plural forms and the file
format are the `Forma.Framework.Localization` README's; the checks run over the files are the
`Forma.Locali` linter's.

## Purpose

A locale file stands for part of a type's name, and its sections complete the rest. Which part a
file stands for has so far been read from its name alone, in a fixed set of forms: a file beside
a folder, inside it under the folder's own name, named for the type, or named for the assembly.
Those forms are good defaults and poor limits. A project may want every folder's files gathered in
one place, a project that borrows another assembly's namespace cannot name a file for its own
assembly and be found, and a host laying its own files over a library's has to guess which of the
forms the library used.

Forma answers this with one mechanism: an ordered list of **paths** written in tokens,
which every source reads its files by, and one canonical form as the default: every folder keeps
its files in a `.locales` folder of its own, one per culture. A project or a host that wants
another form states it.

## Decision summary

- A locale file stands for an **anchor**: the part of a type's name its top level completes from.
  A file's anchor is read from its name against the paths in force, and from nothing else.
- Embedded and host files are read by paths in the same tokens. A source knows every
  file it holds, read once, so finding a type's files never guesses at names.
- The default path is one form, `$RootDir/$AnchorName/$LocalesDir/$Culture$Extension`: a
  folder's own `.locales` folder, holding a file per culture named for the culture alone. A host's
  folder also reads a folder's file lying directly in it, since that folder is itself a folder of
  locale files.
- Configuration lives in `.locali.json` alone, the one settings file the runtime and the linter
  both read. It is embedded in an assembly beside the assembly's locale files, or laid in the
  host's folder, and applies to everything beneath it. A locale file's own `$localization` entry
  carries facts about the file and nothing that steers the loader.
- The outermost `.locali.json` marks a source's root. A source with none is rooted where its
  names begin, so a project laid out by the default needs no settings file.
- A folder of locale files is `.locales` by default and configurable. The leading dot keeps it
  apart from a folder a project already names `Locales`, on disk and in embedded resource names.
- Everything is the library's own. A project embeds its files as it always has; the library adds
  nothing to the build.
- Host entries are applied after every embedded entry, whatever paths either side uses.

## Anchors

Every type is found by its full name, and every file it reads stands for a prefix of that name.
For `Forma.Blazor.Components.Bars.FormaToolbar`, a file standing for `Forma.Blazor` completes the
name with a `Components.Bars.FormaToolbar` section, a file standing for
`Forma.Blazor.Components.Bars` with a `FormaToolbar` section, and a file standing for the type
itself holds its entries at the top level.

An anchor is counted from a **root namespace**, the prefix of a type's name a source completes
from. Below it an anchor is a list of segments, empty for the root itself. The segments are the
parts of a type's full name: namespace segments, a containing type's name for a nested type, and
the type's own name at the deepest anchor. A folder and a namespace have no relation in .NET, and
the library assumes none: it builds names from segments and matches names, and the paths are the
one place the relation between a file's location and the name it completes is stated.

A type reads, in order, every file whose anchor is a prefix of its name, from the root down to the
type. A deeper anchor is closer to the type and wins over a shallower one.

## Paths and tokens

A source reads its files by an ordered list of paths. Each runs from the source's root to a file,
written with `/` between folders and tokens for everything that varies:

| Token | Value |
|---|---|
| `$AssemblyName` | The name of the assembly the type lives in. |
| `$RootNamespace` | The root namespace anchors are counted from. |
| `$RootDir` | Where the source's names begin (see *Roots*), written with `/` between folders. |
| `$AnchorName` | The anchor's segments, joined with `/`; empty at the root. |
| `$ParentName` | The anchor's segments but the last, joined with `/`; empty at the root and one level below it. |
| `$CurrentName` | The anchor's last segment; empty at the root. |
| `$LocalesDir` | The name of a folder of locale files, `.locales` unless a setting says otherwise. |
| `$Culture` | The culture the file is for, such as `he` or `es-419`. |
| `$Extension` | The extension the backend's files carry, such as `.json`. |

A token is `$` followed by a PascalCase name and ends at the first character that is not a
letter, so `$Culture$Extension` is two tokens. `$$` is a literal `$`, as it is in keys. A path
names each token at most once and names `$Culture` and `$Extension` exactly once, and a path
naming a token that does not exist is rejected where it is read. A token's value is written with
`/` between folders too, so `$LocalesDir` may name a nested folder.

A path spells its whole anchor, since a file's name is all a source has to go on: it names
`$AnchorName`, optionally with `$CurrentName`; or `$ParentName` together with `$CurrentName`; or
none of the three, and then stands for the root alone. `$CurrentName` alone would lose every
segment above the last, and `$AnchorName` with `$ParentName` would say the same thing twice.

The tokens `$RootDir`, `$AnchorName`, `$ParentName` and `$CurrentName` can be empty. An empty one
takes the separator before it with it, so `$RootDir/$AnchorName.$Culture` is `$RootDir.$Culture`
at the root, and a name never starts with a separator. A path naming `$ParentName` does not
apply at the root, which has no parent. A file named for its assembly names `$AssemblyName`
outright, since no anchor token stands for it.

### The default paths

Every source reads one path unless a setting states others:

| Path | Reads |
|---|---|
| `$RootDir/$AnchorName/$LocalesDir/$Culture$Extension` | `Components/Bars/.locales/he.json`, a type's own `Components/Bars/FormaToolbar/.locales/he.json`, and `.locales/he.json` at the root |

A host's folder reads two, in the order it applies them at one anchor:

| Path | Reads |
|---|---|
| `$RootDir/$AnchorName/$Culture$Extension` | `Forma.Blazor/Components/Bars/he.json`, and `Forma.Blazor/he.json` at the root |
| `$RootDir/$AnchorName/$LocalesDir/$Culture$Extension` | `Forma.Blazor/Components/Bars/.locales/he.json`, and `Forma.Blazor/.locales/he.json` at the root |

The host's folder is itself a folder of locale files, so the first path reads a file lying
directly in the folder it translates, and the second the file in that folder's own `.locales`,
which is the path every source reads. The two say different things: the outer form speaks for the
host's root as a whole, and the inner one holds translations for its folder alone, so the inner one
wins where both stand for one anchor. Paths a setting declares replace the default ones whole, so a
project that wants another form states it, as
`$RootDir/$AnchorName.$Culture$Extension` reads `Components/Bars.he.json` beside its folder.

### Embedded files and resource names

An embedded file is known by its resource name, which the build forms from the project's root
namespace and the file's path with every `/` made a `.`. A path is read against resource names
the same way, so `Components/Bars/.locales/he.json` under `Forma.Blazor` is
`Forma.Blazor.Components.Bars..locales.he.json`. The doubled dot is the leading dot of `.locales`,
which no namespace segment can produce, so a folder of locale files is never mistaken for a
namespace.

A culture's satellite assembly names a file without its culture, which the build takes only from
just before the extension, as in `Bars.he.json`. A source lists a satellite's files under the
names they would carry in the assembly itself, so every path reads them as it reads any other. A
file whose culture is the whole of its name, as `.locales/he.json`'s is, stays in the assembly.

A project embeds its files with its own item, as it always has. Nothing about a path asks the
build for more.

### Host files

A host's folder is named by `LocalesPath`, `.locales` beside the application by default. A folder
whose name starts with a dot is hidden from a default listing on Linux and macOS and is read like
any other; the source walks its folder with nothing skipped, since .NET treats such names as
hidden and skips them unless told not to.

## Roots

A source needs two things to place anchors: where its names begin, which `$RootDir` stands for,
and the root namespace anchors are counted from, which `$RootNamespace` stands for. The
outermost `.locali.json` in a source fixes both. A source has one root, so two settings files
with none above them are refused. For a host, each assembly's folder is a source of its own.

`$RootDir` is where that file sits, unless the file names `rootDir`: for embedded files, the
prefix the file's own resource name carries before `.locali.json`; for a host's, the folder inside
the host's folder the file was found in. The root namespace is the file's `rootNamespace`, or the
assembly's name when it names none.

A `rootDir` moves where names begin. It is a path relative to the settings file's own folder,
written with `/`, and may climb with `..`; one ending in a project or solution file means that
file's folder, so `./Contoso.csproj` beside a project file is the project's root and
`../../../Contoso.slnx` the solution's. For embedded files the path is walked over the resource
name's segments. No resource name reaches above the project's root namespace, so one climbing
past the settings file's own name is read by the runtime as the root namespace itself. The
runtime cannot see where a project's root namespace ends, and the linter, which can, reports a
`rootDir` climbing past it. For a host's files the path is walked on disk. A root is a folder
inside the host's folder, so one that reaches the host's folder or leaves it is refused. A
settings file naming no `rootDir` leaves names beginning where the file sits, as above.

A source with no `.locali.json` is rooted where its names begin: for embedded files, at the
project's root namespace the build puts at the front of every resource name; for a host's, at the
assembly's folder inside the host's folder. The root namespace is the assembly's name, so a
project whose root namespace is its name needs no settings file.

A type whose name does not start with the root namespace is read by its full name, with the root
namespace left empty: every anchor is a prefix of the full name, which is what a file laid out by
its namespace spells whatever the root. For a host's files `$RootDir` is unchanged, since it names
where files sit rather than what they complete. For embedded files it is empty: the build puts a
namespace at the front of every resource name, so a file laid out by such a type's namespace
begins with it. A path that stands for the root alone does not apply to such a type. A type
outside any namespace belongs to the root, completed from the root namespace like any other.

A nested `.locali.json` applies its `paths` and `localesDir` to the files beneath it and moves
neither root: `$RootDir` and the root namespace are the outermost file's. The runtime passes over
a nested file's `rootNamespace` and `rootDir`, and the linter reports either. A folder holding
strings arranged differently states that in its own paths, with the folder written as literal text
where it sits; the paths are where such a relation is written, and nothing is inferred. A file
above the root, such as one beside a host's assembly folder, is read under the root's settings.

## Settings

### `.locali.json`

The linter and the runtime read the same file, so the two can never disagree about where a file
belongs. Next to the linter's `ignore` and `exclude`, it carries:

| Key | Value |
|---|---|
| `rootNamespace` | The root namespace anchors are counted from beneath the file. |
| `rootDir` | Where the source's names begin, as a path relative to the file's own folder: `strings`, `./Contoso.csproj`, `../../../Contoso.slnx`. One ending in a project or solution file means that file's folder. |
| `localesDir` | The name `$LocalesDir` stands for. |
| `paths` | The paths, in order, replacing the default ones. |

```json
{
  "rootNamespace": "Forma.Blazor.Components.Composer.Plugins",
  "rootDir": "./Forma.AddOns.Composer.Plugins.Explorer.csproj",
  "paths": ["$RootDir/$AnchorName/$LocalesDir/$CurrentName.$Culture$Extension"]
}
```

A file applies to the folder it sits in and everything beneath it, and a nearer file's key wins
over a farther one's. A locale file is read by the paths of the nearest settings file above it.
For embedded files a folder is a resource-name prefix, so a `.locali.json` embedded under
`Forma.Blazor.Components` applies to every file whose name sits below `Components`. The runtime
reads the keys it owns and passes over the linter's; the linter reads both.

A project embeds a `.locali.json` like any other file, and a host lays one in its folder or in an
assembly's folder inside it. One in the host's folder itself puts its `paths` and `localesDir` in
force beneath every assembly's folder. It passes over `rootNamespace` and `rootDir`, since no one
root fits every assembly. A project that keeps its linter rules at the repository's root embeds a
file holding the runtime's keys alone.

### `$localization`

A locale file's metadata entry carries facts about the file — `SchemaVersion`, `Culture`,
`Author`, `Revision` — and nothing that steers the loader. Where a file belongs is the paths' to
say and the file's name to spell, so a file declares no root and no path of its own.

## Precedence

For one type and one culture, entries are applied in this order, each overriding the one before:

1. Embedded files, then host files.
2. Within a source, shallower anchors, then deeper ones.
3. Within one anchor, files read by a farther settings file's paths, then a nearer one's, and
   each settings file's in the order of its paths.
4. Within a file, what the file's own ordering rules say: shared entries before a type's own, a
   higher node's shared entries before a deeper node's.

A host therefore overrides a library entry from any file of its own, whichever form the library
chose. Two files of one source standing for one anchor through different paths are both read,
and the linter reports the pair.

## What goes

`ResourcesPath` retires: a path names any folder a project gathers its files under. The fixed
walk over file names retires with it, and so do the forms it read as defaults: a file beside its
folder, inside it under the folder's own name, named for the type, or named for the assembly. A
project keeping one of them states it as a path.
`RootNamespace` leaves a file's metadata for `.locali.json`, and with it the walk over every
namespace prefix a host file might have declared.

## Decisions

| # | Question | Decision |
|---|---|---|
| L1 | Is a file's placement a build setting or a library feature? | A library feature. A project embeds its files as it always has, and the paths are read at runtime, so every consumer gets it by referencing the package. |
| L2 | One canonical form or many? | One canonical form, a folder's own `.locales` folder with a file per culture, as every source's default, and one mechanism for any other form a project or a host states. A host's folder reads a folder's file lying directly in it as well, since that folder is itself a folder of locale files: the outer form speaks for the host's root, the inner one for its folder alone, and the inner one wins. |
| L3 | Why `.locales` rather than `locales`? | A project may already have a folder named `Locales` for another reason, and on Windows `locales` and `Locales` are the same name. The dot also survives into resource names as a doubled dot, which no namespace produces. |
| L4 | Where do settings live? | In `.locali.json` alone, beside the linter's, embedded in an assembly or laid in the host's folder. A locale file's metadata describes the file and steers nothing, so there is one place to look and one file the runtime and the linter read. |
| L5 | What marks a source's root? | Its outermost `.locali.json`; a source with none is rooted where its names begin. |
| L6 | Do a host's paths have to match the library's? | No. Host entries are applied after every embedded entry whatever paths either uses, and a host's `.locali.json` states its own paths. |
| L7 | Can a file be named for an assembly whose root namespace is not its name? | Yes. A path names `$AssemblyName` outright, which a settings file states where a project keeps such a file. |
| L8 | Are the anchor tokens namespaces? | No. An anchor's segments are parts of a type's full name, which take in containing types and the type itself, and the library relates a folder to a name only through the paths. The tokens say *name*. |
