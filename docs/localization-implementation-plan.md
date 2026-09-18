# Forma Localization Implementation Plan

The plan for building the paths [the localization design](localization-design.md) states, and the
record of what has landed. Two repositories take part: this one, and the `Forma.Locali` linter
beside it, which reads the same settings file and resolves files through the same types.

## Status

Steps 1 to 3 are built. Steps 4 to 7 are not.

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
   the default paths. The settings a `.locali.json` carries are read into a record by a reader in
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

2. **The linter reads the runtime's keys.** In `Forma.Locali`, `.locali.json` accepts
   `rootNamespace`, `rootDir`, `localesDir` and `paths` rather than reporting them as unknown,
   holds each to the runtime's own rule, and reports a malformed path through step 1's parser, at
   the token that is wrong. The linter does not yet resolve files by the paths.

   **Gate:** the linter's tests for each key, well-formed and malformed, and the linter over
   `Forma.slnx` still reporting nothing.

   ✅ Built. The rules the runtime holds a value to are one statement it exposes rather than two
   the linter would have to keep in step, so the two can never refuse different files. Two
   departures from what this step planned:

   - **A nested `.locali.json` naming a `rootNamespace` is not reported yet.** The linter walks
     ancestors the way an `.editorconfig` is read and does not yet know where a source begins, so
     which file is outermost *within a source* cannot be told apart from which is outermost in a
     run. The rule moves to step 5, where the linter resolves files through a source.
   - **`rootDir` joined the keys**, which the step was written before.

3. **Sources read by paths.** `LocaleSource` holds the names its store lists, read once, and
   answers a type from the paths in force rather than from a fixed walk: roots found in the order
   the design gives, nested `.locali.json` files applied to the anchors beneath them, and entries
   applied in the design's order. `RootNamespace` leaves a file's metadata: a host names an
   assembly's root in the `.locali.json` of that assembly's folder, and the fixtures that declared
   one move to that.
   `EmbeddedLocaleSource` lists an assembly's resource names and its satellites';
   `HostLocaleSource` walks its folder with nothing skipped. A `rootDir` is resolved here: over
   resource-name segments for embedded files, on disk for a host's, a project or solution file
   standing for its folder. The default paths read every file the walk read.

   **Gate:** every existing localization test passes unchanged. New fixtures and tests for each
   `.locales` form embedded and in the host folder; a file named for an assembly whose root
   namespace differs; a root named by an embedded `.locali.json`, by its name alone and by its
   `rootNamespace`; a `rootDir` naming a folder, a project file and a solution file, and one
   climbing past the root; a nested `.locali.json` changing the paths beneath it; a host file overriding
   an embedded entry from a form the assembly does not use;
   and a `.locales` folder and a `.locali.json` found in the host's folder, run on Linux as well
   as Windows.

   ✅ Built. A source lists its names once, and places each file at the anchor it stands for
   through the matcher, so a type's files are looked up rather than guessed at. An embedded source
   lists an assembly's resource names, plus a culture's satellite names under the names they carry
   in the assembly. A host source lists every file beneath its folder. The build was checked to
   embed `.locali.json` and `.locales` files under the names the design gives them. The fixtures
   that proved a root through embedded settings live in the test support assembly, whose root
   namespace is not its name. A settings file embedded in the JSON test assembly would re-root
   every other fixture there. The existing tests pass unchanged, except where they wrote the
   retired root: the declared-root tests now declare it in a settings file. Rules this step
   settled, and departures from what it planned:

   - **The host's own `.locali.json` applies to every assembly.** A file in the host's folder
     itself puts its `paths` and `localesDir` in force beneath every assembly's folder. The root
     of an assembly's files is still marked inside that assembly's folder, so that file's
     `rootNamespace` and `rootDir` are not read.
   - **Only the file marking the root names it.** A nested file's `rootNamespace` and `rootDir`
     are passed over. Two settings files with none above them are refused as two roots.
   - **A root is always a folder below where names start.** A host's `rootDir` that reaches the
     host's folder or leaves it is refused. An embedded one climbing past the settings file's own
     name lands on the root namespace. The runtime cannot see where a project's root namespace
     ends, and the linter can.
   - **An embedded type outside the root namespace is spelled from the start.** The build puts a
     namespace at the front of every resource name, so such a type's files begin with its own
     namespace rather than beneath `$RootDir`. A host's are still read beneath it. A type outside
     any namespace belongs to the root.
   - **One anchor's files can fall under different settings files.** They are read farther
     folder first, and each folder's files in the order of its paths. A file above the root,
     such as one beside a host's assembly folder, is read under the root's settings.
   - **`ResourcesPath` applies only where no settings file marks the root**, until step 4
     removes it.
   - **The linter's source follows the new members** so its build and tests stay green. It still
     names files by its fixed forms, so this repository's `.locali.json` leaves LOC0112 out for
     the fixtures only a settings file places.
   - **The host tests ran on Windows alone.** They mark the `.locales` folder and the settings
     file hidden, which is what a leading dot means on Linux, but no Linux machine was available
     to run them.

4. **The defaults move.** `LocalesPath` defaults to `.locales`, and `ResourcesPath` is removed;
   its tests become tests of a path naming the folder instead. The test project's and the
   Showcase's host folders are renamed, and the Showcase copies its folder to the output under the
   new name.

   **Gate:** the suite green, and the Showcase's localization page reading its host section in
   `fr` from the renamed folder, walked in a browser.

5. **The linter resolves by the paths.** In `Forma.Locali`, a project's files are served under
   the names the build embeds them by and read through step 3's source, so the runtime and the
   linter place every file the same way. The host folder is the one the settings name, not a fixed
   `locales`. Four rules join: a file no path in force places, two files of one source standing
   for one anchor, a `.locali.json` naming a `rootNamespace` or `rootDir` that the runtime passes
   over, which only the file marking the root may name, and a `rootDir` climbing past the
   project's root namespace, which the runtime cannot see and reads as the root namespace itself.
   This repository's `.locali.json` drops the LOC0112 entries for the fixtures a settings file
   places.

   **Gate:** the linter's tests for each rule and for a project using `.locales` paths, and
   the linter over `Forma.slnx` reporting nothing.

6. **The repository takes one form.** Every locale file in this repository moves to the one form
   the repository chooses, the repository's `.locali.json` states that form as its paths so the
   linter holds every future file to it, and each project's embedding item reaches the files where
   they now sit.

   The form is chosen at this step. The recommendation is
   `$RootDir/$AnchorName/$LocalesDir/$CurrentName.$Culture$Extension`: the files sit in the
   folder they serve, and every editor tab still names what the file is for, where `he.json` in every folder
   would not.

   **Gate:** the suite green, the linter reporting nothing, and the Showcase walked in every
   culture it routes, including the composer's and the explorer's pages, whose strings come from
   the Pro packages and the AddOn.

7. **Documentation.** The localization README's account of how a file is found becomes the
   paths, the JSON backend's README follows, and `repository-structure.md`, the
   `forma-localizing` skill and the localization test project's README describe the form the
   repository uses. The linter's README gains the runtime's keys and the two rules.

## Deliberately out of scope

- **Watching the host's folder for changes.** A source reads its folder once, as it does today.
- **Paths per culture.** A path names `$Culture` and nothing chooses a path by it.
- **Build-time checks.** The linter is where a misplaced file is reported; the build embeds files
  as it always has.

## Open

Nothing beyond the form step 6 chooses.
