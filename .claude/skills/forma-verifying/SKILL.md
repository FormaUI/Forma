---
name: forma-verifying
description: Forma's build and verification gate — the build, test, format and script-test commands, warnings as failures, the CS1591 sweep for undocumented public API, rebuilding and running the Showcase without serving stale code, inspecting every culture, driving browser checks in a visible window, and the shape of a task script under scripts/. Use when building, testing, formatting, running the Showcase, verifying a change in a browser, or adding a task script.
---

# Build and verification

```powershell
dotnet build Forma.slnx
dotnet test Forma.slnx
dotnet format Forma.slnx --verify-no-changes
npm test --prefix tests/Forma.Blazor.Tests.Scripts
dotnet run --project src/Forma.Blazor.Showcase
```

Every compiler warning is a build failure: `TreatWarningsAsErrors` is on for
src and tests alike, the `.editorconfig` severity table decides what the
compiler reports, and the answer to an unwanted diagnostic is that table,
never a local suppression.

No project sets `GenerateDocumentationFile`, so nothing reports an undocumented public member. Turn it on for one throwaway build to list them:

```powershell
dotnet build Forma.slnx -p:GenerateDocumentationFile=true --no-incremental -v:q -nologo |
    Select-String CS1591
```

- Do require build, tests, and the format check to pass before offering a component for review.
- Do run that `CS1591` sweep after adding or changing public API, and ignore the hits under `tests/`, which are test method names.
- Do run the format check rather than trusting the build: it catches using-directive ordering and line endings that compilation ignores, and it does not read `.razor` files at all, so a misordered `@using` has to be caught by eye.
- Do reopen `Forma.slnx` after adding a project; the C# extension rewrites it with a duplicate `<Folder>` entry and drops `<Configurations>`, which then fails the build with `MSB4025`.
- Do stop a running Showcase process before rebuilding, because it holds its `bin` DLLs open and the copy fails with `MSB3027`.
- Do confirm the rebuild actually reached the app before drawing any conclusion from the browser: the Showcase runs a compiled binary with no hot reload, so verify the build succeeded and that `src/Forma.Blazor.Showcase/bin/Debug/net11.0/Forma.Blazor.dll` is newer than the sources you changed. A stale process silently serves the old code and every experiment against it is void.
- Do inspect the component in all three cultures through the Showcase.
- Do drive end-to-end browser checks in a **visible** window rather than a headless one, so the maintainer can watch what is actually being exercised; a headless run reports only what its script thought to assert, so a mis-scoped page and a genuine pass look alike.
- Do slow such a run enough to be followed, and hold the final state on screen before closing.
- Don't pipe the Showcase command's output into `Select-String`, `Select-Object`, or another filter; closing the pipeline terminates the server.
- Don't present a component as complete while build, tests, accessibility, touch, or direction checks remain unresolved.

## Scripts

Task scripts live in `scripts/` and are PowerShell 7, cross-platform.

- Do name a script `Verb-Noun.ps1` with a verb from PowerShell's approved list (`Get-Verb`) and one PascalCase noun, so `Get-TwemojiPack.ps1`, `Build-GlyphPack.ps1`, `Update-PluralRules.ps1`; a second hyphen or a lowercase `fetch-` style name is wrong.
- Do write a generated data asset compact by default, and take a `-Format` switch that writes it indented for a person reviewing it.
- Do make a script that generates an asset validate its inputs and fail loudly, so a stale or malformed asset cannot be produced silently.
- Don't introduce another runtime for tooling; a script needs only PowerShell and the .NET SDK.
