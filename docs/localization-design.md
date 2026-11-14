# Forma Localization Design

## Scope

How localized strings are read from locale files and resolved. Authoritative for the entries
every file is read into, the default reading of a file's place and name, the paths a project may
state instead, the `.locale.json` config that shapes reading, the locale packs an assembly's
strings may ship in, and the order entries apply in. The contract a component reads strings
through, holes, plural forms and the file format are the `Forma.Framework.Localization` README's;
the checks run over the files are the [Locali](https://github.com/FormaUI/Locali) linter's,
`dotnet locali`.

## Purpose

A project lays its locale files out the way that suits it: a `.locales` folder beside each
namespace's code, one folder per culture holding a file per namespace, a file per type, or a layout
it brings from another system. Which layout it chose never changes what a file means, which text
wins, or what the linter reports.

Forma answers this with one model, the **entry**: a culture, an assembly, a type and a key,
with the text the type shows for that key in that culture. Reading turns every file into
entries, by the default reading or by paths the project states, and reading is the one step
that knows a file's place or name. Everything after it works on entries alone: finding a
string, deciding which source wins, reporting a conflict, listing a type's strings, and the
linter's rules about coverage and holes. An entry remembers the file it came from only to name
it in a message.

## Locales

A locale provides text for a culture. Each entry identifies the assembly, type, and key the text belongs to:

```text
culture · assembly · type · key → text
es · Forma.Blazor · Forma.Blazor.Components.Bars.FormaToolbar · "Bold" → "Negrita"
```

| Part | What it is |
|---|---|
| Culture | The culture the text is for, as .NET names it: `es`, `es-419`, `zh-Hant`. |
| Assembly | The assembly the type lives in. |
| Type | The type's full name. |
| Key | The English source text the type looks up. |
| Text | What the type shows for the key in the culture, or the key itself where a file declares the source text right with a JSON `null`. |

The assembly is part of what an entry is for, because a type's full name doesn't say it:
`Forma.Blazor.Components.Bars.FormaToolbar` lives in `Forma.Blazor`, and
`Forma.Blazor.Components.DataView.FormaDataGrid` in `Forma.Blazor.DataView`, two assemblies whose
types share the start of a namespace. The runtime looks a type's strings up assembly by assembly.

An entry also has a **source**, the place that ships it: the type's own assembly, a locale
pack, an application, or the folder beside the running application. The source decides precedence,
which [Precedence](#precedence) sets out.

A source reads an assembly's files for a culture once, at the first lookup that needs them, and
keeps the entries they hold. The first lookup in a culture therefore reads every file the
assembly has for it; every later lookup in that culture finds an entry already read.

## Reading a file

A file is read into entries by its place, its name and its sections. Every source applies the
default reading below, unless a project states [paths](#paths) for its own files.

1. **Culture.** A folder named for a culture, or the last part of the file's name:
   `.locales/es/Accounts.Admin.json`, `.locales/es.json`, `FormaToolbar.es.json`. A file in a
   culture folder carries no culture in its name, so the last part of its name is part of the
   type's name. A culture is written as .NET names it and compared exactly, so the type `He` is
   never the culture `he`, and the linter reports a part of a name that is a culture in another
   case and names no type, as `ES` is.
2. **Name.** The folders from where the assembly's names begin, which [Roots](#roots) sets out,
   down to the file, then the file's name, leaving out folders of locale files, the culture and the
   extension. A folder of locale files is `.locales` unless the assembly's `localesDir` names
   another. A folder and a dot separate a name alike, so `Components/Bars/.locales/es.json`,
   `.locales/es/Components.Bars.json` and `.locales/es/Components/Bars.json` all name
   `Components.Bars`.
3. **Assembly.** The name counts from the assembly's root namespace, so `Components.Bars` in
   `Forma.Blazor` names `Forma.Blazor.Components.Bars`. In an application or a pack, a name that
   starts with an assembly's name holds that assembly's strings, which
   [Another assembly's strings](#another-assemblys-strings) sets out.
4. **Sections.** A file's name is a prefix of a type's full name, and its sections complete it, one
   segment or several at a time: a file named for `Components.Bars` holds a `FormaToolbar` section,
   a file named for `Components.Bars.FormaToolbar` holds that type's keys directly, and the root's
   file can hold `Components.Bars.FormaToolbar` as one section. A section whose name starts with
   `@.` completes the name from the root namespace instead, wherever its file sits. A file's
   `$shared` section holds keys every section of the file reads unless the section gives its own.

The parts of a name are the parts of a type's full name: namespace segments, a containing type's
name for a nested type, and the type's own name last. A folder is a namespace, never a type: a
type's own file is named for the type, as `Components/Bars/FormaToolbar.es.json` or
`.locales/es/Components.Bars.FormaToolbar.json` is. The runtime reads a folder named for a type all
the same, and the linter reports it.

A name can fit two readings. In an application whose root namespace is `Epica.App`,
`.locales/es/Forma.Blazor.Components.Bars.json` names the application's own
`Epica.App.Forma.Blazor.Components.Bars` and `Forma.Blazor`'s `Components.Bars` alike. A lookup
reads a file for the type it looks up, so each reading applies where its type exists, and the
linter reports a name that fits both.

### Another assembly's strings

An application words other assemblies' strings its own way, and adds languages they don't ship, in
files whose names start with the assembly's name. The culture folder sits before the assembly's name
or after it, and the name goes on from that assembly's root namespace:

```text
.locales/es/Forma.Blazor/Components.Bars.json      { "FormaToolbar": { "Bold": "Negritas" } }
.locales/Forma.Blazor/es/Components.Bars.json      the same
.locales/Forma.Blazor/Components/Bars/es.json      the same
.locales/es/Forma.Blazor.DataView.json             { "FormaDataGrid": { "Sort By": "Ordenar Por" } }
```

The last file names `Forma.Blazor.DataView`, whose root namespace is
`Forma.Blazor.Components.DataView`, so its section names
`Forma.Blazor.Components.DataView.FormaDataGrid`. A pack writes its files the same way, every name
starting with an assembly's.

Rewording another assembly's strings is an application's choice, so the runtime reads such names in
an application and in a pack, and the linter reports one in a library.

An assembly named for a culture would make `.locales/es/` both a culture folder and that assembly's
folder. The culture wins, and the linter reports the assembly's name where the assembly is built.

## Paths

A project may state another layout as paths in its `.locale.json`, which replace the default
reading for its files. A path is notation, as the default reading is: it says where a file sits and
how its name spells a type's, and what a file read by it holds is the same entries. A path
reads a layout the default reading has no rule for, such as a culture inside a folder's name,
`strings-es/Accounts.Admin.json`, read by `$RootDir/strings-$Culture/$AnchorName$Extension`.

A path runs from the source's root to a file, written with `/` between folders and tokens for
everything that varies:

| Token | Value |
|---|---|
| `$AssemblyName` | The name of the assembly the type being read lives in. |
| `$RootNamespace` | The root namespace names are counted from. |
| `$RootDir` | Where the source's names begin, which [Roots](#roots) sets out, written with `/` between folders. |
| `$AnchorName` | The name the file stands for, its segments joined with `/`; empty at the root. |
| `$ParentName` | The name's segments but the last, joined with `/`; empty at the root and one level below it. |
| `$CurrentName` | The name's last segment; empty at the root. |
| `$LocalesDir` | The name of a folder of locale files, `.locales` unless `localesDir` says otherwise. |
| `$Culture` | The culture the file is for, such as `he` or `es-419`. |
| `$Extension` | The extension the backend's files carry, such as `.json`. |

A token is `$` followed by a PascalCase name and ends at the first character that is not a letter,
so `$Culture$Extension` is two tokens. `$$` is a literal `$`, as it is in keys. A path names each
token at most once and names `$Culture` and `$Extension` exactly once, and a path naming a token
that does not exist is rejected where it is read. A token's value is written with `/` between
folders too, so `$LocalesDir` may name a nested folder.

A path spells the whole name its file stands for: it names `$AnchorName`, optionally with
`$CurrentName`; or `$ParentName` together with `$CurrentName`; or none of the three, and then stands
for the root alone. The tokens `$RootDir`, `$AnchorName`, `$ParentName` and `$CurrentName` can be
empty. An empty one takes the separator before it with it, so `$RootDir/$AnchorName.$Culture` is
`$RootDir.$Culture` at the root, and a name never starts with a separator. A path naming
`$ParentName` does not apply at the root, which has no parent.

A path that names `$AssemblyName` reads the strings of whichever assembly the type being read lives
in, so in an application it reads other assemblies' strings as well as the application's own.

## Embedded files and resource names

The build embeds every locale file itself, through the targets the library ships, so a project
names none. A locale file is a JSON file carrying the `$localization` entry, so the build finds the
files by that entry wherever they sit in the project, outside its build output and outside any
folder whose name starts with a dot but `.locales`, and embeds each once.

An embedded file is known by its resource name: the project's root namespace, then the file's path
with every `/` made a `.`, as `Forma.Blazor.Components.Bars..locales.he.json`. The build names
every file this way itself, since the SDK's own naming would make a culture folder `es-419` into
`es_419`. A name is read against resource names with `.` standing for a folder, which is why a
folder and a dot separate a name alike. The doubled dot is the leading dot of `.locales`, which no
namespace segment can produce, so a folder of locale files is never mistaken for a namespace.

A file the project embeds through an item of its own is left to that item, under the name the item
gives it, and so is a file an item in the project file copies beside its output, which stays a file
there; an embedded file is no content, so a server never also copies it. A culture's satellite
assembly names a file without its culture, which the build takes only from just before the
extension, as in `Bars.he.json`, and a source lists a satellite's files under the names they would
carry in the assembly itself.

## The application's files

An application's locale files are a source of their own, after every pack, holding its own strings
and its strings for other assemblies alike. An application is a WebAssembly client, a server built
on the Web SDK, or a Blazor Hybrid application, which the build knows as an application referencing
its platform's web view package, WPF's, Windows Forms' or MAUI's, its output an executable or, on
Android, the project the Android SDK marks as an application. A WebAssembly client has no file
system to read a folder from and an application MAUI packages has none beside it, so the
application's files are embedded as every assembly's are.

A Web App's server renders its client's components too, so it reads its client's files as well as
its own. As the server builds, it asks each project it references whether that project is an
application that embedded locale files, and records each one that did with
`LocaleReferenceAttribute`, naming its assembly; the server reads them before its own.

The runtime finds the application by its entry assembly or, where the runtime names none, as a
native host such as .NET for Android's or iOS's names none, among the loaded assemblies: the one its
build marked as the application, which only an application's build does. After the application's
own files it reads the folder beside the running application, which
`LocalizationOptions.LocalesPath` names, `.locales` beside the application by default. No build
fills that folder; a host places files in it to change a string without building again, and it has
the last word. The folder is read by the same rule, with folders where an embedded name has dots. A
folder whose name starts with a dot is hidden from a default listing on Linux and macOS and is read
like any other.

## Roots

Reading needs two things: where an assembly's names begin, which `$RootDir` stands for, and the
root namespace names are counted from, which `$RootNamespace` stands for. The assembly's
`.locale.json` fixes both, and it sits at the assembly's root: embedded at the project's root, so
its resource name is the project's root namespace and `.locale.json`, and nowhere else. An assembly
has one config file, so a second one anywhere among its files is refused, and the linter reports
one embedded beneath the root.

`$RootDir` is where that file sits, the project's root namespace as the build spells it, unless the
file names `rootDir`: a folder beneath it, written with `/`, where the assembly's names begin, so
`Strings` begins them at the root namespace followed by `Strings`, and the `Strings` folder is
never part of a name. The root namespace is the file's `rootNamespace`, or the assembly's name when
it names none. An assembly with no `.locale.json` is rooted at the project's root namespace, and its
root namespace is its name.

A name starting with another assembly's name goes on from that assembly's root namespace, which the
runtime carries over from the assembly's own embedded `.locale.json`. The root namespace is the
assembly's fact, stated once: an application states nothing about another assembly's names, and a
`.locale.json` laid beside its files for another assembly configures nothing, which the linter
reports.

A type whose name does not start with the root namespace is read by its full name, with the root
namespace left empty, so a file laid out by such a type's namespace spells its whole name. A type
outside any namespace belongs to the root.

A `.locale.json` applies to the whole assembly and nowhere else. Nothing above it is layered under
it and nothing beneath it overrides it, because it is a runtime file the assembly ships rather than
a compile-time file a tree cascades, as `.editorconfig` does.

## Locale packs

An assembly may embed its own locale files, and an application its own. A **locale pack** is a
third source beside them: an assembly carrying locale files for the types of *other* assemblies,
shipped as its own package.

What a pack is for is a language that need not be in the assembly it localizes. Forma's own
localizations ship in packs, one per language, so a host installs the languages it wants and the
assemblies it installs carry the language their keys are written in. A host that would rather
embed its own strings does so as with any library.

### A pack is an application's strings for other assemblies, shipped as an assembly

An application's files for other assemblies start every name with an assembly's name. A pack is
that arrangement embedded in an assembly of its own, so it is read by the same rule and needs no
mechanism of its own:

```text
Forma.Blazor/Components/Bars/he.json
Forma.Blazor/Components/Inputs/.locales/he.json
Forma.Blazor.Composer/he.json
Forma.Blazor.Explorer/he.json
```

Each file is embedded under its own path with `/` made `.`, which is the name the runtime reads it
by, so a pack's resource names spell the tree a reader sees in the repository. A pack holds no
`.locale.json`: where an assembly's names begin is that assembly's own fact, stated once in the
assembly, and each name goes on from the root namespace the runtime carries over.

A pack declares what it carries, as `<LocalePackCulture>he</LocalePackCulture>` or several
separated by semicolons, and the build refuses a pack that declares nothing and a file of any
culture but the declared, taking a file's culture as the runtime does, from a culture folder or the
file's name, so one package is one language by construction. The declaration is written into the
assembly as metadata, where a tool reading a built pack finds it. The runtime reads a pack for one
culture at a time, so a pack holding no file for a culture yields nothing for it.

### A pack's name means nothing to the runtime

A pack is found by name and read by its resources, and no part of either is parsed for meaning. An
assembly named `Forma.AddOns.Resources.Office.Locale.Chinese`, one named
`Forma.AddOns.Resources.Office.Chinese` and one named for nothing in particular are read
identically, and a pack for Forma's assemblies can be published by someone Forma has never heard
of. Which assemblies a pack localizes is said by the names of its files and nowhere else.

Forma's own packs are named `Forma.Blazor.Locales.{Language}`, for the product family they carry.
Forma owns every assembly it localizes, so one package per language is one thing for a host to
install: a pack carries as many assemblies' strings as it has files for.

### An application names its packs, not a library

The attribute that records a pack is written into the project holding the reference, and the packs
that are read are the ones the application names, its entry assembly or, where the runtime names
none, the assembly its build marked. Which languages ship is therefore the application's decision
and cannot be made for it by a library it installs, so a library never references a pack.

A class library that references a pack is stamped itself rather than stamping the application, so
its pack is not found from the application. A host arranged that way names its packs through
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
`PackageReference` and no code. A pack named in an attribute but missing from the output is
reported and left out, so a deployment that lost a language still renders in the language its keys
are written in.

## Configuration

### `.locale.json`

The runtime's config file carries the runtime's keys and nothing else. The linter reads it through
the runtime's own reader, so the two can never disagree about what a file holds, and keeps its own
`localeSources`, `ignoreDiagnostics` and `excludeFiles` in `.locali.json`, which the runtime never
reads. The keys:

| Key | Value |
|---|---|
| `rootNamespace` | The root namespace the assembly's names are counted from. |
| `rootDir` | The folder beneath the assembly's root where its names begin, written with `/`: `Strings`, `Localization/Strings`. |
| `localesDir` | The name of a folder of locale files, which `$LocalesDir` stands for. |
| `paths` | The paths the assembly's files are read by, in order, replacing the default reading. |

```json
{
  "rootNamespace": "Forma.Blazor.Components.Composer.Plugins"
}
```

A file applies to every file of its assembly, and an assembly with none reads by the defaults. The
runtime passes over a key it does not read, so a file written for a later runtime still loads; the
linter reports such a key, since a misspelled one steers nothing. An assembly's `.locale.json` sits
at the project root, and the build embeds it whenever it is there.

### `$localization`

A locale file's metadata entry carries facts about the file, `SchemaVersion`, `Culture`, `Author`
and `Revision`, and nothing that steers reading. What a file holds is its place's and its name's to
say, so a file declares no root and no path of its own.

## Precedence

For one type, one key and one reader's culture, the text is chosen in this order:

1. **The culture.** The reader's culture, then each of its parents: `es-419`, then `es`. A more
   specific culture wins over a later source, so an `es-419` reader sees a library's `es-419` text
   over an application's `es` text.
2. **The source.** The type's own assembly, then each locale pack in the order the application's
   attributes name them, then the clients the application names in the order its attributes name
   them, then the application itself, then the folder beside the running application. A later
   source wins, so an application rewords a library, and a pack overrides what the assembly ships,
   since a host installing a pack asked for it.
3. **Within one source,** an entry given two different texts is a conflict, whatever files and
   layouts gave them. The linter reports it as an error, and the runtime keeps the text from the
   file whose name sorts last, ordinally, so the outcome never depends on the order files are
   found, and logs the conflict once as a warning naming both files, for a host that runs no
   linter. Two files giving one entry the same text aren't a conflict.
4. **Within one file,** what the file's own rules say: a section's own key wins over the file's
   `$shared` one, and a deeper node's shared entries over a higher node's.

## Decisions

| # | Question | Decision |
|---|---|---|
| L1 | Is reading a file a build setting or a library feature? | A library feature. The build embeds every locale file wherever it sits, and files are read at runtime, so every consumer gets it by referencing the package. |
| L2 | What is canonical? | The entry: a culture, an assembly, a type and a key, with its text. Every layout, the default reading or paths a project states, is notation read into entries, and everything after reading works on entries alone, so where a file sits never changes what it means, which text wins or what the linter reports. |
| L3 | Why `.locales` rather than `locales`? | A project may already have a folder named `Locales` for another reason, and on Windows `locales` and `Locales` are the same name. The dot also survives into resource names as a doubled dot, which no namespace produces. |
| L4 | Where does the runtime's configuration live? | In `.locale.json`, one optional file per assembly, embedded at the assembly's root. The linter's own configuration is a separate `.locali.json`: a runtime file an assembly ships and a compile-time file a tool reads are two concerns, and a locale file holds localized strings alone, so neither carries the other's keys. |
| L5 | What marks an assembly's root? | Its `.locale.json`, which sits at the assembly's root and nowhere else; an assembly with none is rooted at its project's root namespace and read by the defaults. |
| L6 | Does an application's file for another assembly follow that assembly's layout? | No. It is read by the same rule as every file, its name starting with the assembly's name, whatever layout the assembly chose for its own files; the entry is the one currency every source trades in. |
| L7 | Can a file be named for an assembly whose root namespace is not its name? | Yes. A name starting with the assembly's name goes on from its root namespace, and a stated path names `$AssemblyName` outright. |
| L8 | Are a name's parts namespaces? | No. They are parts of a type's full name, which take in containing types and the type itself; a folder is related to a name only by the reading. |
| L9 | Does `.locale.json` cascade? | No. It is per assembly: one file, applying to every file of the assembly, with nothing layered above or beneath it. A cascade is how a compile-time file such as `.editorconfig` reaches a tree, and a runtime file an assembly ships has no tree to reach. |
| L10 | Does an application configure another assembly's names? | No. A name starting with an assembly's name goes on from the root namespace that assembly's own file declares, which the runtime carries over. The root namespace is the assembly's fact, stated once, and an application repeating it would give one fact two places to drift apart. |
| L11 | Can a file name a type it does not sit under? | Yes, with a top-level section whose name starts with `@.`, counted from the root namespace as a name is; a type outside the root namespace is spelled in full. It names one type or namespace exactly, never a pattern, so the linter can prove it exists and two types sharing a short name never share a file by accident. |
| L12 | Is a folder ever a type? | No. A folder is a namespace; a type's own file is named for the type, so a file always says what it localizes by its name or its sections. The runtime reads a type-named folder all the same, and the linter reports it. |
| L13 | Where do an assembly's locale files ship? | In the assembly, in a **locale pack** of its own, or both. A pack is an addition rather than a replacement: an assembly embedding its own files is an ordinary arrangement. Forma's own `he` and `es` ship in `Forma.Blazor.Locales.Hebrew` and `Forma.Blazor.Locales.Spanish`, so the assemblies a host installs carry the language their keys are written in and a host installs the languages it wants. |
| L14 | Is a pack a source of its own kind? | A source of its own in the order of precedence, read by the same rule as every file: an application's strings for other assemblies, shipped as an assembly, with no config file of its own. |
| L15 | How is a pack found? | By an assembly attribute the pack's own targets file stamps the referencing project with, which also roots the pack for the trimmer. A pack declares no type, so a reference to it survives nowhere else: the compiler records none, and a trimmed publish deletes the assembly. A host writes a `PackageReference` and no code. |
| L16 | Does the runtime read anything out of a pack's name? | No. A pack is loaded by name and read by its resources, neither of which is parsed, so any assembly can be a pack whatever it is called, for whichever assemblies its files name, published by anyone. `Forma.Blazor.Locales.{Language}` is this repository's convention for its own. |
| L17 | What does a project write to embed its locale files? | Nothing. The build finds them by the `$localization` entry every locale file carries and embeds each once, under the name its path forms, so configuration stays in `.locale.json` and `LocalizationOptions` and no fact about a locale file lives in a project file; a file a project embeds through an item of its own keeps that item's name. |
| L18 | Where does an application keep its strings for other assemblies, and how do they reach a runtime with no file system? | Among its locale files, in names starting with the assembly's name. The build embeds them as it embeds every locale file, so a WebAssembly client, a MAUI application and a server read them from the application's assembly. |
| L19 | How does a Web App's server read its client's files? | Through `LocaleReferenceAttribute`, which the server's build writes for each referenced application that embedded locale files, naming its assembly; the server reads them before its own. The runtime finds the application by its entry assembly or, with none, by the mark its build writes into the application alone. |
| L20 | What decides which text a reader sees? | The culture first, then the source, and nothing about a file's place or name. A more specific culture wins over a later source, and a later source over an earlier one. |
| L21 | What happens when one source gives an entry two texts? | It is a conflict: the linter reports it as an error, and the runtime keeps the text from the file whose name sorts last, ordinally, so the outcome is fixed while the error stands, and logs the conflict once as a warning naming both files, since a host that runs no linter would otherwise show one of two texts without knowing. The same text twice isn't a conflict. |
| L22 | Can a culture be a folder? | Yes. A folder named for a culture holds files for it, a file per namespace or per type named with dots, so a project keeps one folder per language instead of a `.locales` folder per namespace. |
| L23 | How is a culture told from a part of a name? | A culture is written as .NET names it and compared exactly, so `he` is a culture and the type `He` is not; a file in a culture folder carries no culture in its name. The build, the runtime and the linter decide it the same way, and the linter reports a part of a name that is a culture in another case and names no type, which would otherwise be read as part of a name in silence. |
| L24 | What if an assembly is named for a culture? | The culture wins wherever the two meet, and the linter reports the assembly's name where it is built, the one place the name can change. |
| L25 | Do stated paths remain? | Yes. A path is notation read into the same entries, and it reads layouts the default reading has no rule for, so a project bringing files from another system keeps them where they are. A project's paths replace the default reading for its files. |
| L26 | When is a file read? | Once per source and culture, at the first lookup that needs it; every later lookup finds an entry already read. |
