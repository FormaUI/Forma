# Forma Localization Implementation Plan

The plan for building the paths [the localization design](localization-design.md) states, and the
record of what has landed. Two repositories take part: this one, and the `Forma.Locali` linter
beside it, which reads the same config file and resolves files through the same types.

## Status

Steps 1 to 4 and 6 to 8 are built. Step 5 is built in part: the linter reads every file as the
runtime does, and two of its rules are not written yet.

## Steps

Each step ends buildable, tested and reviewable, and passes the gate: `dotnet build Forma.slnx
--no-incremental`, `dotnet test Forma.slnx`, `dotnet format Forma.slnx --verify-no-changes`,
`npm test --prefix tests/Forma.Blazor.Tests.Scripts`, the linter over `Forma.slnx`, and, where a
step changes what the Showcase reads, a visible-browser walk of its pages in `en`, `he`, `es` and
`fr`. A step in the linter's repository passes that repository's own build and tests as well.

1. **Paths and their settings.** `Forma.Framework.Localization` gains the paths as types a
   source and a tool both use: a path parsed from text, rejecting an unknown token or a path
   without `$Culture` and `$Extension`; the value of every token for an anchor; the name a path
   gives an anchor, as a file path and as a resource name; the anchor a path reads from a name; and
   the default paths. The settings a `.locale.json` carries are read into a record by a reader in
   the same package, which passes over keys it does not own, so the linter's `ignore` and
   `exclude` never trouble it, and holds `rootDir` as written for a source to resolve. A locale
   file's `$localization` gains nothing: it describes the file, and the paths are the settings
   file's to state.

   No source reads any of it yet, so nothing a type reads changes.

   **Gate:** tests for every token at the root, one level down and at a type; the empty-token rule;
   both directions of every default path, as a file path and as a resource name; the doubled dot of
   `.locales`; rejected paths; and the settings reader over a file carrying the linter's keys
   too.

   ✅ Built. The path syntax is read by a parser beside the field and expression parsers, which
   reports what is wrong through the same diagnostics, at the position it is written, so the linter
   can point at it; the path, its binder and matcher, and the settings reader sit with the other
   file types. One departure from what this step planned:

   - **The design gained three rules** that writing the parser forced: a path spells its whole
     anchor or none of it, names each token at most once, and never starts a name with a
     separator.

   The tokens were renamed while this step was under review, so that none claims to be a
   namespace: an anchor's segments are parts of a type's full name, and `$AnchorName`,
   `$ParentName` and `$CurrentName` say so, beside `$RootDir` for where names begin. The schema
   version is untouched.

2. **The linter reads the runtime's keys.** In `Forma.Locali`, `.locale.json` accepts
   `rootNamespace`, `rootDir`, `localesDir` and `paths` rather than reporting them as unknown,
   holds each to the runtime's own rule, and reports a malformed path through step 1's parser, at
   the token that is wrong. The linter does not yet resolve files by the paths.

   **Gate:** the linter's tests for each key, well-formed and malformed, and the linter over
   `Forma.slnx` still reporting nothing.

   ✅ Built. The rules the runtime holds a value to are one statement it exposes rather than two
   the linter would have to keep in step, so the two can never refuse different files. Two
   departures from what this step planned:

   - **A nested `.locale.json` naming a `rootNamespace` is not reported yet.** The linter walks
     ancestors the way an `.editorconfig` is read and does not yet know where a source begins, so
     which file is outermost *within a source* cannot be told apart from which is outermost in a
     run. The rule moves to step 5, where the linter resolves files through a source.
   - **`rootDir` joined the keys**, which the step was written before.

3. **Sources read by paths.** `LocaleSource` holds the names its store lists, read once, and
   answers a type from the paths in force rather than from a fixed walk: roots found in the order
   the design gives, nested `.locale.json` files applied to the anchors beneath them, and entries
   applied in the design's order. `RootNamespace` leaves a file's metadata: a host names an
   assembly's root in the `.locale.json` of that assembly's folder, and the fixtures that declared
   one move to that.
   `EmbeddedLocaleSource` lists an assembly's resource names and its satellites';
   `HostLocaleSource` walks its folder with nothing skipped. A `rootDir` is resolved here: over
   resource-name segments for embedded files, on disk for a host's, a project or solution file
   standing for its folder. The default paths read every file the walk read.

   **Gate:** every existing localization test passes unchanged. New fixtures and tests for each
   `.locales` form embedded and in the host folder; a file named for an assembly whose root
   namespace differs; a root named by an embedded `.locale.json`, by its name alone and by its
   `rootNamespace`; a `rootDir` naming a folder, a project file and a solution file, and one
   climbing past the root; a nested `.locale.json` changing the paths beneath it; a host file overriding
   an embedded entry from a form the assembly does not use;
   and a `.locales` folder and a `.locale.json` found in the host's folder, run on Linux as well
   as Windows.

   ✅ Built. A source lists its names once, and places each file at the anchor it stands for
   through the matcher, so a type's files are looked up rather than guessed at. An embedded source
   lists an assembly's resource names, plus a culture's satellite names under the names they carry
   in the assembly. A host source lists every file beneath its folder. The build was checked to
   embed `.locale.json` and `.locales` files under the names the design gives them. The fixtures
   that proved a root through embedded settings live in the test support assembly, whose root
   namespace is not its name. A config file embedded in the JSON test assembly would re-root
   every other fixture there. The existing tests pass unchanged, except where they wrote the
   retired root or its reader: the declared-root tests now declare it in a config file, a file's
   record lost its second argument, the tests that read a file's root were dropped, and the
   metadata tests call the check by its new name. Rules this step settled, and departures from
   what it planned:

   - **The host's own `.locale.json` applies to every assembly.** A file in the host's folder
     itself puts its `paths` and `localesDir` in force beneath every assembly's folder. The root
     of an assembly's files is still marked inside that assembly's folder, so that file's
     `rootNamespace` and `rootDir` are not read.
   - **Only the file marking the root names it.** A nested file's `rootNamespace` and `rootDir`
     are passed over. Two config files with none above them are refused as two roots.
   - **A root is always a folder below where names start.** A host's `rootDir` that reaches the
     host's folder or leaves it is refused. An embedded one climbing past the config file's own
     name lands on the root namespace. The runtime cannot see where a project's root namespace
     ends, and the linter can.
   - **An embedded type outside the root namespace is spelled from the start.** The build puts a
     namespace at the front of every resource name, so such a type's files begin with its own
     namespace rather than beneath `$RootDir`. A host's are still read beneath it. A type outside
     any namespace belongs to the root.
   - **One anchor's files can fall under different config files.** They are read farther
     folder first, and each folder's files in the order of its paths. A file above the root,
     such as one beside a host's assembly folder, is read under the root's settings.
   - **`ResourcesPath` applies only where no config file marks the root**, until step 4
     removes it.
   - **A file still declaring `RootNamespace` is read.** The field is passed over like any other
     this build does not know, and the linter's unknown-field rule reports it.
     `LocalizationMetadata.Read` became `Validate`, since nothing is left for it to return.
   - **The linter's source follows the new members** so its build and tests stay green. It still
     names files by its fixed forms, so this repository's `.locali.json` leaves LOC0112 out for
     the fixtures only a config file places.
   - **The host tests ran on Linux as well as Windows**, on Ubuntu under WSL, where a leading dot
     hides a folder from .NET. On both systems the hidden-name tests fail when the source skips
     hidden entries, so they guard the rule rather than pass by accident.

4. **The defaults move.** `LocalesPath` defaults to `.locales`, and `ResourcesPath` is removed;
   its tests become tests of a path naming the folder instead. The test project's and the
   Showcase's host folders are renamed, and the Showcase copies its folder to the output under the
   new name.

   **Gate:** the suite green, and the Showcase's localization page reading its host section in
   `fr` from the renamed folder, walked in a browser.

   ✅ Built. A project names the folder its own files sit in through the paths of a
   `.locale.json` beside them, so the tests that set `ResourcesPath` read the same fixture through
   a config file under `Resources/`. The JSON test assembly gained an empty root config file
   for that, which keeps the one under `Resources/` nested. Departures from what this step
   planned:

   - **The linter tells a host's files from embedded ones by the project's items.** It skipped a
     folder named `locales` by its name alone, and `.locales` at a project root is a host's folder
     in one project and an embedded form in another. The linter now reads each project's
     `EmbeddedResource` items and lints those alone, whatever folder they sit in, which step 5
     planned to decide.
   - **The linter's alias names retire with `ResourcesPath`.** It read a file under every name
     `ResourcesPath` could have given it, and left alone a name two files claimed that way. A file
     now has the one name the build embeds it by, as the runtime reads it.
   - **The Showcase copies its folder with an item of its own.** The SDK's own globs pass over a
     folder whose name starts with a dot, so the rename alone would have stopped the folder
     reaching the output. The page's text and its translations name the new folder.

5. **The linter resolves by the paths.** In `Forma.Locali`, a project's files are served under
   the names the build embeds them by and read through step 3's source, so the runtime and the
   linter place every file the same way. Four rules join: a file no path in force places, two files of one source standing
   for one anchor, a `.locale.json` naming a `rootNamespace` or `rootDir` that the runtime passes
   over, which only the file marking the root may name, and a `rootDir` climbing past the
   project's root namespace, which the runtime cannot see and reads as the root namespace itself.
   This repository's `.locali.json` drops the LOC0112 entries for the fixtures a config file
   places.

   **Gate:** the linter's tests for each rule and for a project using `.locales` paths, and
   the linter over `Forma.slnx` reporting nothing.

   Built in part, together with step 6. The linter takes each file's name from the build's own
   naming targets, a `LogicalName` and a satellite's culture included, rather than rebuilding it
   from the folder, serves the `.locale.json` files a project embeds to the walk beside its locale
   files, and reads a culture from a file named for the culture alone as well as from one whose
   name ends in it. A file no path in force places is reported under LOC0112, naming the anchors a
   file stands for as the runtime's own source places it, so this repository's `.locali.json`
   leaves LOC0112 out for the one fixture whose folder's settings leave it unread on purpose. The
   other three rules are still to write.

6. **One form, the library's default.** Every source reads
   `$RootDir/$AnchorName/$LocalesDir/$Culture$Extension` unless a setting states otherwise, every
   locale file in this repository moves to it, and each project's embedding item reaches the files
   where they now sit.

   **Gate:** the suite green, the linter reporting nothing, and the Showcase walked in every
   culture it routes, including the composer's and the explorer's pages, whose strings come from
   the Pro packages and the AddOn.

   ✅ Built. Departures from what this step planned:

   - **The form is the library's default, not a repository setting.** The files sit in the
     folder they serve, named for the culture alone, and every source reads that form without a
     `.locale.json`, so this repository states no paths of its own. The forms the library read
     before are no longer defaults; a project keeping one states it.
   - **A host reads two forms.** The host's folder is itself a folder of locale files, so beside
     the default it reads a folder's file lying directly in it, the outer form speaking for the
     host's root and the inner one for its folder alone, the inner one winning where both stand for
     one anchor.
   - **Files that shared a folder merged.** Where a folder held a file for its category and files
     for single types, they became one file per culture with a section per type.
   - **The satellite form keeps a fixture.** A file whose name ends in its culture is the one form
     the build moves into a satellite assembly, so a fixture folder's own settings read it beside
     its type, once kept in the assembly and once in the satellite.

7. **Documentation.** The localization README's account of how a file is found becomes the
   paths, the JSON backend's README follows, and `repository-structure.md`, the
   `forma-localizing` skill and the localization test project's README describe the form the
   repository uses. The linter's README gains the runtime's keys and the two rules.

   ✅ Built with step 6. The fixture README moved beside the JSON test project whose fixtures it
   describes.

8. **Two files, one concern each.** The runtime's configuration moves out of `.locali.json` into
   `.locale.json`, read by `LocaleConfig`: one optional file per assembly, embedded with its files
   or laid in its folder inside the host's, applying to every file of the assembly and cascading
   nowhere, since it is a runtime file the assembly ships. `.locali.json` keeps the linter's
   `ignore` and `exclude` alone and its `.editorconfig`-style cascade, and the runtime never
   reads it. `$tools` leaves the locale files, so a file holds translations and nothing a tool
   reads; a per-file exception is written in `.locali.json`.

   **Gate:** the suites green in both repositories, the linter reporting nothing over
   `Forma.slnx`, and a `.locale.json` the runtime would refuse reported where it is written.

   ✅ Built. Departures from what the earlier steps had settled:

   - **No nested config.** A second `.locale.json` among an assembly's files is refused rather
     than scoped beneath its folder, and a file in the host's folder itself configures no
     assembly. A folder read differently states that in the assembly's paths. The nested rule
     step 5 planned to write is gone with it.
   - **The linter reads `.locale.json` as a file of its own.** LOC0022 reports one the runtime
     would refuse, a key the runtime does not read, or two files for one assembly. LOC0114
     retires with `$tools`.

## Deliberately out of scope

- **Watching the host's folder for changes.** A source reads its folder once, as it does today.
- **Paths per culture.** A path names `$Culture` and nothing chooses a path by it.
- **Build-time checks.** The linter is where a misplaced file is reported; the build embeds files
  as it always has.

## Open

Step 5's two remaining rules: two files of one source standing for one anchor, and a `rootDir`
climbing past the project's root namespace.
