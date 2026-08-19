# locali

A linter for the Forma localization system. It checks translation files against the
schema, and checks `IStringLocale` call sites in C# and Razor against what the runtime
will actually do with them.

The binary is `locali`. The project is not packable, so run it from source:

```powershell
dotnet run --project src/Forma.Framework.Localization.Linter -- --path <target>
```

The examples below use `locali` for the built binary.

## Running

```text
Usage: locali --path <file-or-directory> [--report [<path>]] [--exclude <glob>]
              [--severity <level>]

  --path     The file or directory to lint. A directory is searched recursively.
  --report   Write a JSON report. Defaults to localization-report.json in the
             current directory; a path ending in .json names the file, and any
             other path names the directory to write the default name into.
  --exclude  Leave every file whose path matches the glob unread. May be given
             more than once. Matching is case-insensitive against the whole
             path written with forward slashes, and '*' spans separators, so
             '*/Fixtures/*' skips a folder wherever it sits.
  --severity The lowest severity to report, 'error' or 'warning'. Defaults to
             'error', so a warning neither prints nor fails the run; the
             summary still says how many were left out.
```

`--path` accepts a `.json` file, a `.cs` or `.razor` file, a `.csproj`, a `.sln`/`.slnx`,
or a directory. A directory is searched for `.json`, `.cs`, and `.razor`, skipping `bin`
and `obj`. Source files are linted through the project that owns them, found by walking
up to the nearest `.csproj`.

`--exclude` is what makes a test tree lintable. A test project's fixtures are often
malformed on purpose — a file with no `$localization` entry, a culture that disagrees with
its own name — so linting them reports what the fixture exists to prove. Excluding the
folder they live in leaves the rest of the tree checked:

```powershell
locali --path Forma.slnx --exclude "*/Fixtures/*"
```

`--severity` sets the lowest severity a run reports. An error is something the runtime
will get wrong; a warning is something worth changing that still loads. Only errors are
reported by default. A diagnostic below the level is left out of the console, the JSON
report, and the exit code alike, and the summary line counts what it left out:

```powershell
locali --path Forma.slnx --exclude "*/Fixtures/*" --severity warning
```

| Exit code | Meaning |
|-----------|---------|
| 0 | Every file linted clean at the reported severity |
| 1 | At least one diagnostic was reported |
| 2 | The command line was wrong, or a path could not be read |

Console output is MSBuild-formatted (`path(line,col): severity CODE: message`). The JSON
report carries `fileCount`, `diagnosticCount`, `suppressedCount`, and a `diagnostics` array
of `path`, `line`, `column`, `severity`, `code`, `message`.

## Razor call sites

Almost every call site in this repository lives in `.razor`, which reaches Roslyn only
through the Razor source generator. The linter runs that generator itself, so nothing has
to be emitted to disk and no build property is needed:

```powershell
locali --path MyProject.csproj
```

Call sites are found semantically where the compilation binds `IStringLocale`, and
syntactically otherwise. The syntactic path cannot resolve overloads, so the rules that
count values stay silent for it.

### Why Roslyn is loaded from the SDK

This part is a workaround. Roslyn refuses to load a generator built against a newer
compiler than the host, reporting `ReferencesNewerCompiler` and returning no generators at
all. The Razor generator ships inside the SDK and is built against the SDK's Roslyn, which
is ahead of anything published:

| | Version |
|---|---|
| Razor generator, from the SDK | built against Roslyn 5.9.0 |
| Newest `Microsoft.CodeAnalysis.CSharp` on nuget.org | 5.6.0 |

So the linter cannot load the generator using the packages it compiles against, and
`GetSourceGeneratedDocumentsAsync` returns nothing for Razor. What closes the gap is that
the SDK also carries a complete, version-matched Roslyn — compiler, workspaces and
`Workspaces.MSBuild` — under `DotnetTools`, shipped for `dotnet format` and `dotnet watch`.

`SdkRoslyn` binds to that copy from a `[ModuleInitializer]`, since resolution has to be
registered before the first Roslyn type loads, which is earlier than `Main`. The three
`Microsoft.CodeAnalysis.*` packages carry `ExcludeAssets="runtime"` so their assemblies
stay reference-only and cannot shadow the SDK's at run time.

Redirecting only part of the stack does not work: a 5.6.0 `Workspaces` on a 5.9.0 compiler
throws `TypeLoadException` on members added in the newer version. It is all or nothing.

### Removing it

`DotnetTools` is an SDK implementation detail, not a documented redistribution point, so
this should not outlive the version gap. Once a Roslyn at or above the SDK's is on
nuget.org:

1. Raise the three `Microsoft.CodeAnalysis.*` versions.
2. Drop their `ExcludeAssets="runtime"`.
3. Delete `Projects/SdkRoslyn.cs`, and register MSBuild in `MSBuildProjectLoader` again.

Nothing else depends on it, and the fallback below stays either way.
### When Roslyn cannot be bound

The linter stops with exit code 2 and explains what it looked for. It cannot degrade:
`ExcludeAssets="runtime"` keeps the published assemblies out of the output, so if the
SDK's are not found there is no Roslyn to run on at all. Failing at startup is what keeps
that from surfacing as an unrelated `FileNotFoundException` several frames later.

LOC0206 covers the narrower case where Roslyn did load but a project produced no generated
Razor. The linter then reads whatever a build left in `obj/**/generated/`, which needs
`EmitCompilerGeneratedFiles=true` and a full build — an incremental one emits nothing while
still reporting success. LOC0206 says that route came up empty too, so the project's markup
went unchecked; do not ignore it in a build gate.

## Diagnostics

A file rule reports in the 100s, a call-site rule in the 200s.

### Translation files (LOC0100–LOC0199)

| Code | Severity | Reports |
|------|----------|---------|
| LOC0100 | Error | The file is not readable JSON |
| LOC0101 | Error | The file carries no `$localization` entry |
| LOC0102 | Warning | The metadata entry is not the file's first property |
| LOC0103 | Error | The metadata entry does not follow the field grammar |
| LOC0104 | Error | The metadata entry omits a required field |
| LOC0105 | Warning | The metadata entry declares its required fields out of order |
| LOC0106 | Error | The file is written for a schema this build does not define |
| LOC0107 | Warning | The metadata entry declares an unknown field |
| LOC0108 | Error | The file declares a culture its own name does not give it |
| LOC0109 | Error | A translation fills a hole its key does not name |
| LOC0110 | Error | A translation writes text shaped like a hole the runtime cannot read |
| LOC0111 | Error | The file declares the same key more than once |
| LOC0112 | Error | No type exists with the full name a translation file's own location implies |

LOC0112 covers the drift a rename opens: `JsonStringLocaleFactory` resolves a file by the
owning type's full name, never by where the file sits, so moving a folder or renaming a type
leaves the translations stranded while every other check stays green. The rule reads the name
the default embedding convention gives a file and asks whether any type answers to it,
allowing for the leading segments `ResourcesPath` inserts, the arity a generic's metadata name
carries, and the missing root of a type in the global namespace. A file the project embeds
under an explicit `LogicalName` is beyond it, because that decouples the file's position from
its logical name entirely.

LOC0109 catches the silent case: `"Stored {stored}."` translated as `"…{stord}…"` renders
`{stord}` literally. A name may be written as a brace hole or as a plural expression's
bracketed argument, and the two are equivalent, so mixing them across a key and its
translation is legal. Reordering or dropping a name is legal.

### Call sites (LOC0200–LOC0299)

| Code | Severity | Reports | Runtime |
|------|----------|---------|---------|
| LOC0200 | Error | An interpolated hole is not a plain identifier | throws |
| LOC0201 | Error | An expression reads a name nothing supplies | counts as 0, silently |
| LOC0202 | Error | The value count does not match the key's distinct holes | throws |
| LOC0203 | Error | An expression naming no argument is given no value | throws |
| LOC0204 | Error | A key carries a malformed expression | renders as written, silently |
| LOC0205 | Warning | A plural expression omits `other` | renders nothing, silently |
| LOC0206 | Warning | A project's Razor markup went unchecked | — |
| LOC0207 | Error | A named value the key never reads | the hole renders as written, silently |
| LOC0208 | Error | A key writes text shaped like a hole the runtime cannot read | renders as written, silently |

All three ways of filling holes are checked. An interpolated key and a positional call
name their values after the key's holes; a named call writes the names out, and those are
read from the source:

```csharp
locale[$"Stored {stored} of {total}."]                              // LOC0200
locale["Stored {stored} of {total}.", written, allowed]             // LOC0202
locale["Stored {stored} of {total}.", ("stored", a), ("total", b)]  // LOC0207
```

A name is read from a tuple, from `new LocalizedArgument(…)`, and from
`LocalizedArgument.Unnamed(…)`. One value whose name is not written out — a variable, or a
collection passed whole — leaves the whole list unjudged, and the rules stay silent.

### Where a rule stays silent

Silence below is the rule working. A false positive in a build gate blocks correct code,
so a rule fires only where the failure is certain.

Reporting these would flag code that is right:

- **A hole repeated twice counts once.** `"{n} of {n}"` takes one value, because that is
  how the runtime pairs them. Counting it twice would report every correct call.
- **A hole no value fills renders as written.** `Resolve` returns a pattern with its holes
  still open for the caller to fill, so an unfilled hole is the feature, not a mistake.
  A name supplied for a hole that does not exist is the certain half, and LOC0207 has it.
- **Text shaped like a hole, containing a space, is prose.** `{ color: red }`, `{ }` and
  `{colour, red}` render as written and were meant to. LOC0110 and LOC0208 take the other
  half, where the text could only have been a hole: `{0}`, `{a.b}`, and `{stored,x}`, whose
  name the runtime reads but whose alignment is not a number, so the hole goes unread.

Nothing at the call site decides these, so there is nothing to judge:

- **A values collection passed whole.** `object?[]` into `params ReadOnlySpan<object?>`
  converts, so the items arrive as the values and their count is whatever the collection
  holds at run time. An `int[]` in the same position cannot convert and arrives as one
  boxed value, which is why that one *is* counted.
- **An overload nothing resolved.** Only the syntactic path reaches this, and only when
  the compilation did not bind the call. Build with the references in place and the
  semantic path takes over.

## Layout

| Folder | Holds |
|--------|-------|
| `Cli/` | Argument parsing, options, exit codes, target resolution |
| `Diagnostics/` | `LintCode`, the `LintCodes` catalogue, `LintDiagnostic` |
| `Documents/` | Reading a translation file into ordered entries with offsets |
| `Linting/` | The services that drive file and schema linting |
| `Projects/` | MSBuild loading, and binding Roslyn to the SDK's copy |
| `Reporting/` | Console and JSON reporters |
| `Rules/` | Rule contracts, rule contexts, shared hole-name reading |
| `Rules/Files/` | Rules over a translation file |
| `Rules/Sources/` | Rules over a call site |
| `Sources/` | Finding call sites and reading their keys and arguments |

## Adding a rule

1. Add a `LintCode` to `Diagnostics/LintCodes.cs` and to its `All` list, numbered in the
   100s for a file rule or the 200s for a call-site rule.
2. Implement `ILocalizationFileRuleChecker` or `ICallSiteRuleChecker` under
   `Rules/Files/` or `Rules/Sources/`. Expose the code through `Code` and report through
   the context, which owns the position.
3. Register it in `Program.cs`.
4. Register it in the matching test factory under `tests/…/Support/`.

`LintCodesTests` fails if a rule reports under a code missing from the catalogue, if two
codes share an id, if two rules in the same family report under the same code, or if a
rule reports outside its family's range. The range rule means the same idea checked on
both sides of a call — a key and a translation, say — needs two codes, one per family, as
`TranslationHoleShapedText` (LOC0110) and `HoleShapedText` (LOC0208) show. A rule that
needs to report two different things is two rules regardless of family.

`HoleNames` reads the names a format refers to its values by, and matches the runtime's
`LocalizedFormat.Names`. Use it rather than scanning for braces; a plural expression names
its argument in brackets, and escaped braces are literal text.

A rule that depends on how values reach the call site should read
`context.Site.Arguments` and return when it is `Unknown`.

## Build and test

```powershell
dotnet build Forma.slnx
dotnet test tests/Forma.Framework.Localization.Linter.Tests
dotnet format Forma.slnx --verify-no-changes
```

`SourceLinterTestFactory.Lint` compiles a class body against the real
`IStringLocale` and returns what the call-site rules report, so a rule can be tested
against a bound compilation rather than a stub.

Run a new rule over `src/Forma.UI.Blazor` and `src/Forma.UI.Blazor.Showcase` before
trusting it; both lint clean, so anything it reports there is a false positive. They do
not cover every shape a call site can take — nothing in `src` passes named
`LocalizedArgument` values or a values collection whole — so those belong in unit tests.