# Forma Localization Design

## Scope

How locale files are laid out and found. Authoritative for the paths a source reads its files by,
the tokens a path is written in, the `.locale.json` config that shapes it, the locale packs an
assembly's locale files may ship in instead, and the order entries are applied in. The contract a
component reads strings through, holes, plural forms and the file format are the
`Forma.Framework.Localization` README's; the checks run over the files are the `Forma.Locali`
linter's.

Out of scope: a source reads its folder once and watches it for no change; a path names
`$Culture` and nothing chooses a path by it; and a misplaced file is the linter's to report,
since the build embeds files the ordinary way.

## Purpose

A locale file stands for part of a type's name, and its sections complete the rest. Which part a
file stands for is stated rather than guessed from a fixed set of names: a project may want every
folder's files gathered in one place, a project that borrows another assembly's namespace has to
be able to name a file for its own assembly and be found, and a host laying its own files over a library's
must not have to guess which form the library used.

Forma answers this with one mechanism: an ordered list of **paths** written in tokens,
which every source reads its files by, and one canonical form as the default: every folder keeps
its files in a `.locales` folder of its own, one per culture for the folder and one per culture
for a type or namespace beneath it. A project that wants another form states it; a host never
needs to.

## Anchors

Every type is found by its full name, and every file it reads stands for a prefix of that name.
For `Forma.Blazor.Components.Bars.FormaToolbar`, a file standing for `Forma.Blazor` completes the
name with a `Components.Bars.FormaToolbar` section, a file standing for
`Forma.Blazor.Components.Bars` with a `FormaToolbar` section, and a file standing for the type
itself holds its entries at the top level.

An anchor is counted from a **root namespace**, the prefix of a type's name a source completes
from. Below it an anchor is a list of segments, empty for the root itself. The segments are the
parts of a type's full name: namespace segments, a containing type's name for a nested type, and
the type's own name at the deepest anchor, each written as a C# identifier is, so a folder whose
name could not be part of one is never read as a segment; nor is the folder of locale files,
whatever `$LocalesDir` names it, since that folder completes no name. A folder is a namespace,
never a type: a type's own file is named for the type among its namespace folder's locale files,
and a folder named for a type, which the runtime would read all the same, is the linter's to
report. A folder and a namespace have no relation in .NET, and
the library assumes none: it builds names from segments and matches names, and the paths are the
one place the relation between a file's location and the name it completes is stated.

A type reads, in order, every file whose anchor is a prefix of its name, from the root down to the
type. A deeper anchor is closer to the type and wins over a shallower one.

A section completes the name from the file's anchor, one segment or several at a time. A section
at the file's top level whose name starts with `@.` completes it from the root namespace instead,
so `@.Components.Bars.FormaToolbar` names that type from any file of the assembly, wherever the file
sits; for a type outside the root namespace it spells the full name, as the folders do. Such a
section is placed at the anchor it names, after the files whose position places them there. The
source finds rooted sections by reading every file the paths place in a culture once; a file that
cannot be read carries none, and reports what is wrong with it when its own type is read.

## Paths and tokens

A source reads its files by an ordered list of paths. Each runs from the source's root to a file,
written with `/` between folders and tokens for everything that varies:

| Token | Value |
|---|---|
| `$AssemblyName` | The name of the assembly the type lives in. |
| `$RootNamespace` | The root namespace anchors are counted from. |
| `$RootDir` | Where the source's names begin (see *Roots*), written with `/` between folders. |
| `$AnchorName` | The anchor's segments, each a part of a type's name, joined with `/`; empty at the root. |
| `$ParentName` | The anchor's segments but the last, joined with `/`; empty at the root and one level below it. |
| `$CurrentName` | The anchor's last segment; empty at the root. |
| `$LocalesDir` | The name of a folder of locale files, `.locales` unless a setting says otherwise; never a segment of an anchor. |
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

Every source reads two paths unless a setting states others, both into a folder's own `.locales`:

| Path | Reads |
|---|---|
| `$RootDir/$AnchorName/$LocalesDir/$Culture$Extension` | The folder's file: `Components/Bars/.locales/he.json`, and `.locales/he.json` at the root |
| `$RootDir/$ParentName/$LocalesDir/$CurrentName.$Culture$Extension` | A file named for a type or namespace beneath the folder: `Components/Bars/.locales/FormaToolbar.he.json` |

A host's folder reads the same two directly inside the folder as well:

| Path | Reads |
|---|---|
| `$RootDir/$AnchorName/$Culture$Extension` | `Forma.Blazor/Components/Bars/he.json`, and `Forma.Blazor/he.json` at the root |
| `$RootDir/$ParentName/$CurrentName.$Culture$Extension` | `Forma.Blazor/Components/Bars/FormaToolbar.he.json` |

The host's folder is itself a folder of locale files, so inside it the `$LocalesDir` step is already
taken: a file sits in the folder's own `.locales` or directly in the folder, and the two forms
resolve equally, at one anchor, with no precedence between them, so a file in both places is a
duplicate rather than an override. Paths a setting declares replace the default ones whole, so a
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

A project embeds its files with its own item. Nothing about a path asks the build for more.

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
than a compile-time file a tree cascades, as `.editorconfig` does. A folder
holding strings arranged differently states that in the assembly's paths, with the folder written
as literal text where it sits; the paths are where such a relation is written, and nothing is
inferred.

## Locale packs

An assembly may embed its own locale files, and a host writes its own in its folder. A **locale
pack** is a third source beside them: an assembly carrying locale files for the types of *other*
assemblies, shipped as its own package. It takes nothing from either of the other two.

What a pack is for is a language that need not be in the assembly it localizes. Forma's own
localizations ship in packs, one per language, so a host installs the languages it wants and the
assemblies it installs carry the language their keys are written in. A host that would rather
embed its own strings, or lay them in its folder, does so as with any library.

### A pack is a host's folder that ships as an assembly

A host's folder already holds locale files for assemblies other than the one it belongs to: a
folder per assembly, laid out by the root namespace that assembly declares, and the source reading
it holds no config of its own. A pack is that arrangement embedded in an assembly instead of
sitting on disk, so it is read by the paths a host's folder is read by and needs no mechanism of
its own:

```
Forma.Blazor/Components/Bars/he.json
Forma.Blazor/Components/Inputs/.locales/he.json
Forma.Blazor.Composer/he.json
Forma.Blazor.Explorer/he.json
```

Each file is embedded under its own path with `/` made `.`, which is the name the runtime reads it
by, so a pack's resource names spell the tree a reader sees in the repository. A pack holds no
`.locale.json`: where an assembly's names begin is that assembly's own fact, stated once in the
assembly, and the pack lays each folder out by the root namespace the runtime carries over, exactly
as a host's folder does.

Nothing at runtime reads a pack's culture. A source is read for one culture at a time and a pack
holding no file for it yields nothing, so a pack carrying one language, several, or two related
cultures such as `es` beside `es-419` is read correctly without the loader knowing anything about
any of them. A pack declares what it carries all the same, as
`<LocalePackCulture>he</LocalePackCulture>` or several separated by semicolons, and the build
refuses a pack that declares nothing and a file of any culture but the declared, so one package
is one language by construction. The declaration is written into the assembly as metadata, where
a tool reading a built pack finds it.

### A pack's name means nothing to the runtime

A pack is found by name and read by its resources, and no part of either is parsed for meaning. An
assembly named `Forma.AddOns.Resources.Office.Locale.Chinese`, one named
`Forma.AddOns.Resources.Office.Chinese` and one named for nothing in particular are read
identically, and a pack for Forma's assemblies can be published by someone Forma has never heard
of. Which assemblies a pack localizes is said by the folders inside it and nowhere else.

Forma's own packs are named `Forma.Blazor.Locales.{Language}`, for the product family they carry.
Forma owns every assembly it localizes, so one package per language is one thing for a host to
install: a pack carries as many assemblies' folders as it has files for.

### An application names its packs, not a library

The attribute that records a pack is written into the project holding the reference, and the packs
that are read are the ones the entry assembly names. Which languages ship is therefore the
application's decision and cannot be made for it by a library it installs, so a library never
references a pack.

A class library that references a pack is stamped itself rather than stamping the application, so
its pack is not found from the entry assembly. A host arranged that way names its packs through
`LocalizationOptions`, which is also how a test reaches a pack without depending on what its runner
made the entry assembly.

### Finding a pack

A pack declares no type, so nothing a host writes names one. The compiler therefore records no
reference to it, `GetReferencedAssemblies` never lists it, nothing loads it, and a trimmed publish
deletes it. Referencing a pack is a fact of the build, and the build is where it has to be written
down.

A project is a pack because it says `<IsLocalePack>true</IsLocalePack>`, which ships with the
library and makes a project of nothing but locale files into one, whoever writes it. A pack's
package then carries a targets file, which MSBuild imports into the project referencing it. It
stamps that project with an assembly attribute naming the pack, and roots the pack for the trimmer.
The application's own assembly then says which packs it was built against, in its own metadata, and
`AddJsonLocalization` reads those attributes and loads each pack named. A host writes one
`PackageReference` and no code: there is no registrar to call and no language to name twice. A pack
named in an attribute but missing from the output is reported and left out, so a deployment that
lost a language still renders in the language its keys are written in.

## Configuration

### `.locale.json`

The runtime's config file carries the runtime's keys and nothing else. The linter reads it through
the runtime's own reader, so the two can never disagree about where a file belongs, and keeps its
own `localeSources`, `ignoreDiagnostics` and `excludeFiles` in `.locali.json`, which the runtime
never reads. The keys:

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

An assembly's `.locale.json` sits at the project root, and the build embeds it whenever it is
there, through the targets the library ships, so a project names it nowhere. A host lays none: its
folders are laid out by what each assembly declares.

### `$localization`

A locale file's metadata entry carries facts about the file — `SchemaVersion`, `Culture`,
`Author`, `Revision` — and nothing that steers the loader. Where a file belongs is the paths' to
say and the file's name to spell, so a file declares no root and no path of its own.

## Precedence

For one type and one culture, entries are applied in this order, each overriding the one before:

1. An assembly's own embedded files, then each locale pack in the order the entry assembly's
   attributes name them, then host files.
2. Within a source, shallower anchors, then deeper ones.
3. Within one anchor, files in the order of the assembly's paths, then the files whose rooted
   sections name the anchor.
4. Within a file, what the file's own ordering rules say: shared entries before a type's own, a
   higher node's shared entries before a deeper node's.

A host therefore overrides a library entry, from a pack or from the assembly itself, with any file
of its own, whichever form either chose. A pack in turn overrides what the assembly ships, since a
host installing a pack asked for it. Two files of one source standing for one anchor through
different paths are both read, and the linter reports the pair.

## Decisions

| # | Question | Decision |
|---|---|---|
| L1 | Is a file's placement a build setting or a library feature? | A library feature. A project embeds its files the ordinary way, and the paths are read at runtime, so every consumer gets it by referencing the package. |
| L2 | One canonical form or many? | One canonical form with two files: a folder's own `.locales` folder holding the folder's file per culture and, beside it, a file per culture named for a type or namespace beneath the folder, as every source's default, and one mechanism for any other form a project states. A host's folder is itself a folder of locale files, so inside it both may sit directly in the folder; the two forms resolve equally, at one anchor, with no precedence between them, and a file in both places is a duplicate. |
| L3 | Why `.locales` rather than `locales`? | A project may already have a folder named `Locales` for another reason, and on Windows `locales` and `Locales` are the same name. The dot also survives into resource names as a doubled dot, which no namespace produces. |
| L4 | Where does the runtime's configuration live? | In `.locale.json`, one optional file per assembly, embedded at the assembly's root. The linter's own configuration is a separate `.locali.json`: a runtime file an assembly ships and a compile-time file a tool reads are two concerns, and a locale file holds translations alone, so neither carries the other's keys. |
| L5 | What marks an assembly's root? | Its `.locale.json`, which sits at the assembly's root and nowhere else; an assembly with none is rooted where its names begin and read by the defaults. |
| L6 | Do a host's paths have to match the library's? | No. A host's folder is always the canonical form, a folder per anchor with its files in the folder's own `.locales` or directly in it, whatever paths the assembly states for its own files; the anchor is the one currency both sides trade in, so an override or a contribution is written the same way for every assembly, and host entries are applied after every embedded entry. |
| L7 | Can a file be named for an assembly whose root namespace is not its name? | Yes. A path names `$AssemblyName` outright, which a project keeping such a file states in its config. |
| L8 | Are the anchor tokens namespaces? | No. An anchor's segments are parts of a type's full name, which take in containing types and the type itself, and the library relates a folder to a name only through the paths. The tokens say *name*. |
| L9 | Does `.locale.json` cascade? | No. It is per assembly: one file, applying to every file of the assembly, with nothing layered above or beneath it. A cascade is how a compile-time file such as `.editorconfig` reaches a tree, and a runtime file an assembly ships has no tree to reach. |
| L10 | Does a host configure an assembly's folder? | No. A host's folder holds no `.locale.json`; each assembly's folder is laid out by the root namespace the assembly's own file declares, which the runtime carries over. The root namespace is the assembly's fact, stated once, and a host repeating it would give one fact two places to drift apart. |
| L11 | Can a file name a type it does not sit under? | Yes, with a top-level section whose name starts with `@.`, counted from the root namespace as the folders are; a type outside the root namespace is spelled in full. It names one type or namespace exactly, never a pattern, so the linter can prove it exists and two types sharing a short name never share a file by accident. |
| L12 | Is a folder ever a type? | No. A folder is a namespace; a type's own file is named for the type among its namespace folder's locale files, so a file always says what it localizes by its name or its sections, and never by a folder a reader has to know is a type. The runtime reads a type-named folder all the same, and the linter reports it. |
| L13 | Where do an assembly's locale files ship? | In the assembly, in a **locale pack** of its own, or both. A pack is an addition rather than a replacement: an assembly embedding its own files is an ordinary arrangement. Forma's own `he` and `es` ship in `Forma.Blazor.Locales.Hebrew` and `Forma.Blazor.Locales.Spanish`, so the assemblies a host installs carry the language their keys are written in and a host installs the languages it wants. |
| L14 | Is a pack a source of its own kind? | It is a source of its own, reading what a host's folder reads: the same paths, one folder per assembly laid out by that assembly's declared root namespace, no config file of its own. The arrangement a host already writes is the one a pack ships. |
| L15 | How is a pack found? | By an assembly attribute the pack's own targets file stamps the referencing project with, which also roots the pack for the trimmer. A pack declares no type, so a reference to it survives nowhere else: the compiler records none, and a trimmed publish deletes the assembly. A host writes a `PackageReference` and no code. |
| L16 | Does the runtime read anything out of a pack's name? | No. A pack is loaded by name and read by its resources, neither of which is parsed, so any assembly can be a pack whatever it is called, for whichever assemblies it holds folders for, published by anyone. `Forma.Blazor.Locales.{Language}` is this repository's convention for its own. |
