# Forma Localization Design

## Scope

How locale files are laid out and found. Authoritative for the paths a source reads its files by,
the tokens a path is written in, the `.locale.json` config that shapes it, and the order entries
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
  folder is itself a folder of locale files, so inside it a folder's file may as well sit directly
  in the folder; the two forms resolve equally, at one anchor.
- The runtime's configuration lives in `.locale.json`, one optional file per assembly, embedded
  at the assembly's root. It applies to every file of that assembly and to nothing else: it is a
  runtime file the assembly ships, not a compile-time file a tree cascades. A host's folder holds
  none; each assembly's folder inside it follows what the assembly's own file declares. The
  linter's own configuration is `.locali.json`, which the runtime never reads. A locale file holds
  translations alone: its `$localization` entry carries facts about the file, and nothing in it
  steers the loader or a tool.
- An assembly's `.locale.json` marks where its files begin. An assembly with none is rooted where
  its names begin and read by the defaults, so a project laid out by the default needs no config
  file.
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

A host's folder reads a second path beside it:

| Path | Reads |
|---|---|
| `$RootDir/$AnchorName/$Culture$Extension` | `Forma.Blazor/Components/Bars/he.json`, and `Forma.Blazor/he.json` at the root |

The host's folder is itself a folder of locale files, so inside it the `$LocalesDir` step is already
taken: a folder's file sits in the folder's own `.locales` or directly in the folder, and the two
forms resolve equally, at one anchor, with no precedence between them, so a file in both places is
a duplicate rather than an override. Paths a setting declares replace the default ones whole, so a
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
assembly's `.locale.json` fixes both, and it sits at the assembly's root: embedded at the
project's root, so its resource name is the project's root namespace and `.locale.json`, and
nowhere else. An assembly has one config file, so a second one anywhere among its files is
refused, and the linter reports one embedded beneath the root.

`$RootDir` is where that file sits, the project's root namespace as the build spells it, unless
the file names `rootDir`: a folder beneath it, written with `/`, where the assembly's files
begin, so `Strings` begins them at the root namespace followed by `Strings`. The root namespace
is the file's `rootNamespace`, or the assembly's name when it names none.

A host's folder holds no config file. Each assembly's folder inside it is that assembly's root,
laid out by the root namespace the assembly's own `.locale.json` declares, which the runtime
carries over from the assembly's embedded files. The root namespace is the assembly's fact,
stated once; a `.locale.json` a host lays in its folder, above or inside an assembly's,
configures nothing, and the linter reports it.

An assembly with no `.locale.json` is rooted where its names begin: for embedded files, at the
project's root namespace the build puts at the front of every resource name; for a host's, at the
assembly's folder inside the host's folder. The root namespace is the assembly's name, so a
project whose root namespace is its name needs no config file.

A type whose name does not start with the root namespace is read by its full name, with the root
namespace left empty: every anchor is a prefix of the full name, which is what a file laid out by
its namespace spells whatever the root. For a host's files `$RootDir` is unchanged, since it names
where files sit rather than what they complete. For embedded files it is empty: the build puts a
namespace at the front of every resource name, so a file laid out by such a type's namespace
begins with it. A path that stands for the root alone does not apply to such a type. A type
outside any namespace belongs to the root, completed from the root namespace like any other.

A `.locale.json` applies to the whole assembly and nowhere else. Nothing above it is layered under
it and nothing beneath it overrides it, because it is a runtime file the assembly ships rather
than a compile-time file a tree cascades, as `.locali.json` and `.editorconfig` do. A folder
holding strings arranged differently states that in the assembly's paths, with the folder written
as literal text where it sits; the paths are where such a relation is written, and nothing is
inferred.

## Configuration

### `.locale.json`

The runtime's config file carries the runtime's keys and nothing else. The linter reads it through
the runtime's own reader, so the two can never disagree about where a file belongs, and keeps its
own `ignoreDiagnostics` and `excludeFiles` in `.locali.json`, which the runtime never reads. The
keys:

| Key | Value |
|---|---|
| `rootNamespace` | The root namespace the assembly's anchors are counted from. |
| `rootDir` | The folder beneath the assembly's root where its files begin, written with `/`: `Strings`, `Localization/Strings`. |
| `localesDir` | The name `$LocalesDir` stands for. |
| `paths` | The paths, in order, replacing the default ones. |

```json
{
  "rootNamespace": "Forma.Blazor.Components.Composer.Plugins",
  "paths": ["$RootDir/$AnchorName/$LocalesDir/$CurrentName.$Culture$Extension"]
}
```

A file applies to every file of its assembly, and an assembly with none reads by the defaults. The
runtime passes over a key it does not read, so a file written for a later runtime still loads;
the linter reports such a key, since a misspelled one steers nothing.

A project embeds its `.locale.json` like any other file, at its root, naming it in an item of its
own where its glob passes over a name starting with a dot. A host lays none: its folders are
laid out by what each assembly declares.

### `$localization`

A locale file's metadata entry carries facts about the file — `SchemaVersion`, `Culture`,
`Author`, `Revision` — and nothing that steers the loader. Where a file belongs is the paths' to
say and the file's name to spell, so a file declares no root and no path of its own.

## Precedence

For one type and one culture, entries are applied in this order, each overriding the one before:

1. Embedded files, then host files.
2. Within a source, shallower anchors, then deeper ones.
3. Within one anchor, files in the order of the assembly's paths.
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
`RootNamespace` leaves a file's metadata for `.locale.json`, and with it the walk over every
namespace prefix a host file might have declared. `$tools` leaves the locale files: a locale file
holds translations alone, and what a tool keeps per file belongs in the tool's own file.

## Decisions

| # | Question | Decision |
|---|---|---|
| L1 | Is a file's placement a build setting or a library feature? | A library feature. A project embeds its files as it always has, and the paths are read at runtime, so every consumer gets it by referencing the package. |
| L2 | One canonical form or many? | One canonical form, a folder's own `.locales` folder with a file per culture, as every source's default, and one mechanism for any other form a project states. A host's folder is itself a folder of locale files, so inside it a folder's file sits in the folder's own `.locales` or directly in the folder; the two forms resolve equally, at one anchor, with no precedence between them, and a file in both places is a duplicate. |
| L3 | Why `.locales` rather than `locales`? | A project may already have a folder named `Locales` for another reason, and on Windows `locales` and `Locales` are the same name. The dot also survives into resource names as a doubled dot, which no namespace produces. |
| L4 | Where does the runtime's configuration live? | In `.locale.json`, one optional file per assembly, embedded at the assembly's root. The linter's own configuration is a separate `.locali.json`: a runtime file an assembly ships and a compile-time file a tool reads are two concerns, and a locale file holds translations alone, so neither carries the other's keys and `$tools` is gone. |
| L5 | What marks an assembly's root? | Its `.locale.json`, which sits at the assembly's root and nowhere else; an assembly with none is rooted where its names begin and read by the defaults. |
| L6 | Do a host's paths have to match the library's? | No. Host entries are applied after every embedded entry whatever paths either uses, and a host's `.locale.json` for the assembly states its own paths. |
| L7 | Can a file be named for an assembly whose root namespace is not its name? | Yes. A path names `$AssemblyName` outright, which a project keeping such a file states in its config. |
| L8 | Are the anchor tokens namespaces? | No. An anchor's segments are parts of a type's full name, which take in containing types and the type itself, and the library relates a folder to a name only through the paths. The tokens say *name*. |
| L9 | Does `.locale.json` cascade? | No. It is per assembly: one file, applying to every file of the assembly, with nothing layered above or beneath it. A cascade is how a compile-time file such as `.locali.json` or `.editorconfig` reaches a tree, and a runtime file an assembly ships has no tree to reach. |
| L10 | Does a host configure an assembly's folder? | No. A host's folder holds no `.locale.json`; each assembly's folder is laid out by the root namespace the assembly's own file declares, which the runtime carries over. The root namespace is the assembly's fact, stated once, and a host repeating it would give one fact two places to drift apart. |
