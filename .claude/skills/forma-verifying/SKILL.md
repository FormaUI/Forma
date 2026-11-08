---
name: forma-verifying
description: The build and verification gate — the build, tests, format check, script tests and browser check all passing with warnings as failures, the sweep for undocumented public API, rebuilding and running the showcase without serving stale code, inspecting every culture, driving browser checks in a visible browser window and the gate's own headless, and the shape of a task script. Use when building, testing, formatting, running the showcase, verifying a change in a browser, or adding a task script.
---

# Build and verification

The gate is every check the repository's verification design sets out, each platform's where the machine can run it, all passing, and the showcase for anything that renders. The repository's build documentation lists the commands.

Every warning, the compiler's or MSBuild's, is a build failure: warnings are errors for source, tests and the examples alike, the `.editorconfig` severity table decides what the compiler reports, and the answer to an unwanted diagnostic is that table, never a local suppression.

No project generates a documentation file, so nothing reports an undocumented public member. Turn `GenerateDocumentationFile` on for one throwaway, non-incremental build and filter its output for `CS1591`.

- Do require build, tests, and the format check to pass before offering a component for review.
- Do run the `CS1591` sweep after adding or changing public API, and ignore the hits under the test projects, which are test method names.
- Do run the format check rather than trusting the build: it catches using-directive ordering and line endings that compilation ignores, and it does not read `.razor` files at all, so a misordered `@using` has to be caught by eye.
- Do reopen the solution file after adding a project; the C# extension rewrites it with a duplicate `<Folder>` entry and drops `<Configurations>`, which then fails the build with `MSB4025`.
- Do start the showcase through the repository's showcase script, which builds first and listens on the port it is given; a running one holds its `bin` DLLs open so the copy fails with `MSB3027`, so have the script stop the one already listening rather than finding it by hand. It refuses a port held by anything but this repository's showcase rather than stopping it.
- Do stop MSBuild's worker nodes, through the repository's script for them, when a build fails to copy a file nothing appears to be using: MSBuild keeps them alive between builds and they keep a handle on what they last built. The broader script beside it also stops whatever is running out of this repository, such as the showcase. The language server an editor runs for C# stays running under both, unless the broader one is asked to stop every .NET process on the machine.
- Do confirm the rebuild actually reached the app before drawing any conclusion from the browser: the showcase runs a compiled binary with no hot reload, so verify the build succeeded and that the library's DLL in the showcase's output is newer than the sources you changed. A stale process silently serves the old code and every experiment against it is void.
- Do inspect the component in every culture the library ships through the showcase.
- Do drive end-to-end browser checks in a **visible** browser window rather than a headless one, so the maintainer can watch what is actually being exercised; a headless run reports only what its script thought to assert, so a mis-scoped page and a genuine pass look alike. The gate's browser check is the exception, since the gate runs on machines without a display: it runs headless, and the browser step opens it in a window to watch when asked to.
- Do delay a circuit's inbound events on the server to check a slow connection; Chrome's emulated latency doesn't reach a circuit's WebSocket.
- Do slow such a run enough to be followed, and hold the final state on screen before closing.
- Don't pipe the showcase's output into `Select-String`, `Select-Object`, or another filter; closing the pipeline terminates the server.
- Don't present a component as complete while build, tests, accessibility, touch, or direction checks remain unresolved.

## The build

The build has one entry at the repository's root, with a module and a wrapper script for each narrower loop
beside it, as the repository's build documentation sets out with every command and parameter.

- Do run before each commit the gates the repository's verification design sets for the change's reach, the
  fast gate or the full gate, which runs every platform check the machine can and names each one it couldn't;
  a wrapper beside the module runs one part of either.
- Do narrow a test run to one project, a filter or the build already on disk, and hunt a test that fails
  sometimes by repeating the run past its failures, so the failure shows as a rate; run other test projects
  beside it to load the machine, so a race a busy machine exposes shows here too, and record the waits made
  with diagnostics to list which found their state only after waiting for it.
- Do hunt a browser check that fails sometimes the same way, filtered, repeated and under load, on hosts
  published once, so the run ends with each failing check's rate, and run the check with the others it runs
  beside in the gate, since some checks fail only after their neighbors have run.
- Do clean the task programs' build output too, with the clean step's widest sweep, when the disk is filling:
  a program run from a single .cs file is built into the system temp directory and left there on every edit
  and rename.
- Do name a build script for its action in lowercase, since it is a command someone types; the functions
  in the module take `Verb-Noun` with an approved verb, as PowerShell requires of anything a module
  exports.
- Do put what an action does in the build's module and keep the script a wrapper, so the gate and a single
  action cannot disagree about what a build is, and name the function in the module's manifest, so the
  build's surface grows deliberately.
- Do give every action `SupportsShouldProcess`, so `-WhatIf` describes the run rather than making it.
- Do add a case to the build's own tests for a build script or an argument you add, and to the task scripts'
  contract check for a task script; a step that quietly ran nothing looks exactly like a step that passed,
  and the gate's tooling step runs both and reads every task script, so one nothing covers fails there.
- Do run a step for real against a tree made for the purpose where its body can be wrong, rather than
  only describing it: `-WhatIf` shows which steps run, never what they do, so a bug inside a step's body
  passes every described run.
- Do decide, for a script nothing can reasonably cover, that it is nobody's to maintain and move it to the
  repository's ignored folder for personal work rather than leaving it among the task scripts to rot
  unnoticed.
- Don't let an argument one action reads be ignored by another; refuse it instead.

## Task scripts

Every tooling script the build does not run is a task script: PowerShell 7, cross-platform. What decides which a thing is, is whether the build runs it, so a script that fetches artwork, generates a checked-in asset or drives a browser against a running Showcase is a task script however build-like it sounds.

- Do name a task script `Verb-Noun.ps1` with a verb from PowerShell's approved list (`Get-Verb`) and one PascalCase noun, so `Get-ArtworkPack.ps1` and `Update-PluralRules.ps1`; a second hyphen or a lowercase `fetch-` style name is wrong. A build action is the exception and is lowercase, because it is a word someone types rather than a cmdlet they discover.
- Do open a script with a shebang and `#Requires -Version 7`, run it under `Set-StrictMode -Version Latest`, and give it comment-based help rather than a bare comment, so `Get-Help` answers for it.
- Do write a generated file with the endings it is stored with rather than the machine's, so the same run on another platform produces the same bytes.
- Do reach for PowerShell's own way of doing a thing: a here-string builds a file, not a `StringBuilder` appended to a line at a time.
- Do write a generated data asset compact by default, and take a `-Format` switch that writes it indented for a person reviewing it.
- Do make a script that generates an asset validate its inputs and fail loudly, so a stale or malformed asset cannot be produced silently.
- Don't introduce another runtime for tooling; a script needs only PowerShell and the .NET SDK.
