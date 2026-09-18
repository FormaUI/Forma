# Forma Localization Implementation Plan

The plan for building the paths [the localization design](localization-design.md) states, and the
record of what has landed. Two repositories take part: this one, and the `Forma.Locali` linter
beside it, which reads the same settings file and resolves files through the same types.

## Status

Step 1 is built. Steps 2 to 7 are not.

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

2. **The linter reads the runtime's keys.** In `Forma.Locali`, `.locali.json` accepts `rootNamespace`,
   `localesDir` and `paths` rather than reporting them as unknown, and reports a malformed
   path through step 1's parser, at the path's position. A nested `.locali.json` naming a
   `rootNamespace` is reported, since only the outermost file marks a root. The linter does not
   yet resolve files by the paths.

   **Gate:** the linter's tests for each key, well-formed and malformed, a nested file naming a
   root, and the linter over `Forma.slnx` still reporting nothing.

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

4. **The defaults move.** `LocalesPath` defaults to `.locales`, and `ResourcesPath` is removed;
   its tests become tests of a path naming the folder instead. The test project's and the
   Showcase's host folders are renamed, and the Showcase copies its folder to the output under the
   new name.

   **Gate:** the suite green, and the Showcase's localization page reading its host section in
   `fr` from the renamed folder, walked in a browser.

5. **The linter resolves by the paths.** In `Forma.Locali`, a project's files are served under
   the names the build embeds them by and read through step 3's source, so the runtime and the
   linter place every file the same way. The host folder is the one the settings name, not a fixed
   `locales`. Two rules join: a file no path in force places, and two files of one source
   standing for one anchor.

   **Gate:** the linter's tests for both rules and for a project using `.locales` paths, and
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
