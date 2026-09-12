---
name: forma-verifying
description: The build and verification gate — the build, tests, format check and script tests all passing with warnings as failures, the sweep for undocumented public API, rebuilding and running the showcase without serving stale code, inspecting every culture, driving browser checks in a visible window, and the shape of a task script. Use when building, testing, formatting, running the showcase, verifying a change in a browser, or adding a task script.
---

# Build and verification

The gate is the build, every test project, the format check and the script tests, all passing, and the showcase for anything that renders. The repository's own documentation lists the commands.

Every compiler warning is a build failure: warnings are errors for source and tests alike, the `.editorconfig` severity table decides what the compiler reports, and the answer to an unwanted diagnostic is that table, never a local suppression.

No project generates a documentation file, so nothing reports an undocumented public member. Turn `GenerateDocumentationFile` on for one throwaway, non-incremental build and filter its output for `CS1591`.

- Do require build, tests, and the format check to pass before offering a component for review.
- Do run the `CS1591` sweep after adding or changing public API, and ignore the hits under the test projects, which are test method names.
- Do run the format check rather than trusting the build: it catches using-directive ordering and line endings that compilation ignores, and it does not read `.razor` files at all, so a misordered `@using` has to be caught by eye.
- Do reopen the solution file after adding a project; the C# extension rewrites it with a duplicate `<Folder>` entry and drops `<Configurations>`, which then fails the build with `MSB4025`.
- Do stop a running showcase before rebuilding, because it holds its `bin` DLLs open and the copy fails with `MSB3027`.
- Do confirm the rebuild actually reached the app before drawing any conclusion from the browser: the showcase runs a compiled binary with no hot reload, so verify the build succeeded and that the library's DLL in the showcase's output is newer than the sources you changed. A stale process silently serves the old code and every experiment against it is void.
- Do inspect the component in every culture the library ships through the showcase.
- Do drive end-to-end browser checks in a **visible** window rather than a headless one, so the maintainer can watch what is actually being exercised; a headless run reports only what its script thought to assert, so a mis-scoped page and a genuine pass look alike.
- Do slow such a run enough to be followed, and hold the final state on screen before closing.
- Don't pipe the showcase's output into `Select-String`, `Select-Object`, or another filter; closing the pipeline terminates the server.
- Don't present a component as complete while build, tests, accessibility, touch, or direction checks remain unresolved.

## Scripts

Task scripts are PowerShell 7, cross-platform, in the repository's scripts folder.

- Do name a script `Verb-Noun.ps1` with a verb from PowerShell's approved list (`Get-Verb`) and one PascalCase noun, so `Get-ArtworkPack.ps1` and `Update-PluralRules.ps1`; a second hyphen or a lowercase `fetch-` style name is wrong.
- Do write a generated data asset compact by default, and take a `-Format` switch that writes it indented for a person reviewing it.
- Do make a script that generates an asset validate its inputs and fail loudly, so a stale or malformed asset cannot be produced silently.
- Don't introduce another runtime for tooling; a script needs only PowerShell and the .NET SDK.
