# lintloc

A linter for the Forma localization system. It checks translation files against the
schema, and checks `IStringLocale` call sites in C# and Razor against what the runtime
will actually do with them.

The binary is `lintloc`. The project is not packable, so run it from source:

```powershell
dotnet run --project src/Forma.Framework.Localization.Linter -- --path <target>
```

The examples below use `lintloc` for the built binary.

## Running

```text
Usage: lintloc --path <file-or-directory> [--report [<path>]]

  --path    The file or directory to lint. A directory is searched recursively.
  --report  Write a JSON report. Defaults to localization-report.json in the
            current directory; a path ending in .json names the file, and any
            other path names the directory to write the default name into.
```

`--path` accepts a `.json` file, a `.cs` or `.razor` file, a `.csproj`, a `.sln`/`.slnx`,
or a directory. A directory is searched for `.json`, `.cs`, and `.razor`, skipping `bin`
and `obj`. Source files are linted through the project that owns them, found by walking
up to the nearest `.csproj`.

| Exit code | Meaning |
|-----------|---------|
| 0 | Every file linted clean |
| 1 | At least one diagnostic was reported |
| 2 | The command line was wrong, or a path could not be read |

Console output is MSBuild-formatted (`path(line,col): severity CODE: message`). The JSON
report carries `fileCount`, `diagnosticCount`, and a `diagnostics` array of
`path`, `line`, `column`, `severity`, `code`, `message`.

## Razor call sites need generated C#

Razor emits no C# unless asked, and almost every call site in this repository lives in
`.razor`. Build with `EmitCompilerGeneratedFiles` so the linter can read them:

```powershell
dotnet build MyProject.csproj -p:EmitCompilerGeneratedFiles=true --no-incremental
lintloc --path MyProject.csproj
```

`--no-incremental` matters. An up-to-date project emits nothing, the build still reports
success, and the linter then reports LOC0015 having checked no Razor markup at all.
LOC0015 is the signal that this happened; do not ignore it in a build gate.

Call sites are found semantically where the compilation binds `IStringLocale`, and
syntactically otherwise. The syntactic path cannot resolve overloads, so the rules that
count values stay silent for it.

## Diagnostics

### Translation files

| Code | Severity | Reports |
|------|----------|---------|
| LOC0000 | Error | The file is not readable JSON |
| LOC0001 | Error | The file carries no `$localization` entry |
| LOC0002 | Warning | The metadata entry is not the file's first property |
| LOC0003 | Error | The metadata entry does not follow the field grammar |
| LOC0004 | Error | The metadata entry omits a required field |
| LOC0005 | Warning | The metadata entry declares its required fields out of order |
| LOC0006 | Error | The file is written for a schema this build does not define |
| LOC0007 | Warning | The metadata entry declares an unknown field |
| LOC0008 | Error | The file declares a culture its own name does not give it |
| LOC0016 | Error | A translation fills a hole its key does not name |

LOC0016 catches the silent case: `"Stored {stored}."` translated as `"…{stord}…"` renders
`{stord}` literally. A name may be written as a brace hole or as a plural expression's
bracketed argument, and the two are equivalent, so mixing them across a key and its
translation is legal. Reordering or dropping a name is legal.

### Call sites

| Code | Severity | Reports | Runtime |
|------|----------|---------|---------|
| LOC0009 | Error | An interpolated hole is not a plain identifier | throws |
| LOC0010 | Error | An expression reads a name nothing supplies | counts as 0, silently |
| LOC0011 | Error | The value count does not match the key's distinct holes | throws |
| LOC0012 | Error | An expression naming no argument is given no value | throws |
| LOC0013 | Error | A key carries a malformed expression | renders as written, silently |
| LOC0014 | Warning | A plural expression omits `other` | renders nothing, silently |
| LOC0015 | Warning | A project's Razor markup went unchecked | — |

### What the rules deliberately do not report

Each rule fires only where the failure is certain, because a false positive in a build
gate blocks correct code.

- `{0}`, `{a.b}` and `{ }` in a **literal** key are not holes; they render as written, so
  they are not reported. Only an interpolated hole throws, which is what LOC0009 covers.
- A hole repeated twice counts once, matching how the runtime pairs values.
- Values that carry their own names (`LocalizedArgument`) are not counted against the
  key's holes.
- A values collection passed whole — `object?[]` into `params ReadOnlySpan<object?>` — is
  not counted, because no `params` expansion happens. An `int[]` in the same position
  *is* counted, because it cannot convert and arrives as a single boxed value.
- When the overload cannot be resolved, the counting rules stay silent.

## Layout

| Folder | Holds |
|--------|-------|
| `Cli/` | Argument parsing, options, exit codes, target resolution |
| `Diagnostics/` | `LintCode`, the `LintCodes` catalogue, `LintDiagnostic` |
| `Documents/` | Reading a translation file into ordered entries with offsets |
| `Linting/` | The services that drive file and schema linting |
| `Projects/` | MSBuild loading, including emitted Razor sources |
| `Reporting/` | Console and JSON reporters |
| `Rules/` | Rule contracts, rule contexts, shared hole-name reading |
| `Rules/Files/` | Rules over a translation file |
| `Rules/Sources/` | Rules over a call site |
| `Sources/` | Finding call sites and reading their keys and arguments |

## Adding a rule

1. Add a `LintCode` to `Diagnostics/LintCodes.cs` and to its `All` list.
2. Implement `ILocalizationFileRuleChecker` or `ICallSiteRuleChecker` under
   `Rules/Files/` or `Rules/Sources/`. Expose the code through `Code` and report through
   the context, which owns the position.
3. Register it in `Program.cs`.
4. Register it in the matching test factory under `tests/…/Support/`.

`LintCodesTests` fails if a rule reports under a code missing from the catalogue, if two
codes share an id, or if two rules in the same family report under the same code. A rule
that needs to report two different things is two rules.

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
