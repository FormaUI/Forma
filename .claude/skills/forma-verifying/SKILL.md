---
name: forma-verifying
description: The build and verification gate — the build, tests, format check, script tests and browser check all passing with warnings as failures, the sweep for undocumented public API, rebuilding and running the showcase without serving stale code, inspecting every culture, driving browser checks in a visible browser window and the gate's own headless, and the shape of a task script. Use when building, testing, formatting, running the showcase, verifying a change in a browser, or adding a task script.
---

# Build and verification

Verification answers three questions, each at its own point. While implementing, targeted checks show the fix works where the bug occurred. The **development gate** closes a change set: the checks of everything its commits affect, run against the exact commit being closed. The **full gate**, every check the repository's verification design sets out on every platform it requires, holds the merge of `dev` into `candidate` and a release. The showcase is checked for anything that renders, and the repository's build documentation lists the commands. What to do when the machine gets in the way, a locked file, a rewritten solution, a stale showcase or a filling disk, is in [troubleshooting.md](troubleshooting.md).

Every warning, the compiler's or MSBuild's, is a build failure: warnings are errors for source, tests and the examples alike, the `.editorconfig` severity table decides what the compiler reports, and the answer to an unwanted diagnostic is that table, never a local suppression.

No project generates a documentation file, so nothing reports an undocumented public member. Turn `GenerateDocumentationFile` on for one throwaway, non-incremental build and filter its output for `CS1591`.

- Do require build, tests, and the format check to pass before offering a component for review.
- Do run the `CS1591` sweep after adding or changing public API, and ignore the hits under the test projects, which are test method names.
- Do run the format check rather than trusting the build: it catches using-directive ordering and line endings that compilation ignores, and it does not read `.razor` files at all, so a misordered `@using` has to be caught by eye.
- Do start the showcase through the repository's showcase script, which builds first and listens on the port it is given, stopping the one already listening there.
- Do confirm the running showcase contains the change before interpreting anything in the browser: it runs a compiled binary with no hot reload, so a stale process silently serves the old code and every experiment against it is void.
- Do inspect the component in every culture the library ships through the showcase.
- Do drive interactive browser verification in a **visible** browser window, so whoever watches notices a check exercising the wrong page, which a headless run's assertions alone can't tell from a genuine pass. The gate's browser check runs headless, since the gate runs on machines without a display, and the browser step opens it in a window to watch when asked to.
- Do make a browser check assert the route it landed on and an element that identifies the page, beside the renderer it waits for, so the headless gate catches a check exercising the wrong page on every later run.
- Do delay a circuit's inbound events on the server to check a slow connection; Chrome's emulated latency doesn't reach a circuit's WebSocket.
- Do slow such a run enough to be followed, and hold the final state on screen before closing.
- Don't pipe the showcase's output into `Select-String`, `Select-Object`, or another filter; closing the pipeline terminates the server.
- Don't present a component as complete while build, tests, accessibility, touch, or direction checks remain unresolved.

## The build

The build has one entry at the repository's root, with a module and a wrapper script for each narrower loop
beside it, as the repository's build documentation sets out with every command and parameter.

- Do verify a change by its affected set, as the repository's verification design sets out: the checks of what it
  changed and of everything depending on it, transitively, a shared foundation reaching every component resting on
  it; widen the set wherever its reach can't be determined reliably.
- Do run a change's checks in stages the work chooses and orders, each only once the one before it passes, and
  short-circuit at the first failure: fix it, pass that stage, and go on from there.
- Do order the stages by priority, the cheapest and nearest to the work first: unit tests, a component's bUnit
  tests among them for a component change, then integration tests, then the host, engine or platform the work is
  about, the one a bug was found in or a feature is built for, before any other.
- Do run a bug found in one host alone as its focused reproduction there as soon as the cheapest relevant checks
  pass, ahead of a broader integration run, so the original failure is answered first.
- Do let the context decide how far the cycle goes: a fix to a service is proven in the one host it runs in and stops
  there; a change to how a page behaves goes on, one at a time, to what its reach calls for, the render modes for
  culture routing, navigation, hydration or render-mode-specific behavior, the other engines for browser interop, the
  platform for mobile-specific behavior.
- Do run targeted checks while implementing, in the working tree where nothing else edits or builds what they read,
  and commit each coherent change once its checks pass; never accumulate unrelated changes to split afterward.
- Do close a change set with its development gate: the cumulative affected checks of every commit in it, run
  against the exact commit being closed, on an immutable snapshot, an export or a worktree with its own build
  output, while work goes on in the tree; a set isn't finished until it passes.
- Do count an earlier pass only where it can be tied to the same inputs, the built artifacts and the relevant
  configuration among them, and rerun the check wherever that can't be established: a check whose own code is
  unchanged still reads a coordinator, a published host or a shared build property that may have moved beneath it.
- Do record a failure outside the change and have it investigated independently, by another agent in a worktree of
  its own, blocking the work only where it invalidates the change's verification; never run two checks on the one
  Android emulator at once.
- Do leave the full gate to merging `dev` into `candidate` and to a release, where it's mandatory; a wrapper
  beside the module runs one part of either gate.
- Do leave a platform a local full run couldn't run, which it names, to CI's full run of the same commit: that run
  passes only once every required platform's job has passed, and promotion waits for it; a platform the
  verification design marks as reported rather than required holds nothing.
- Do narrow a test run to one project, a filter or the build already on disk, and hunt a test that fails
  sometimes by repeating the run past its failures, so the failure shows as a rate; run other test projects
  beside it to load the machine, so a race a busy machine exposes shows here too, and record the waits made
  with diagnostics to list which found their state only after waiting for it.
- Do hunt a browser check that fails sometimes the same way, filtered, repeated and under load, on hosts
  built once, so the run ends with each failing check's rate, and run the check with the others it runs
  beside in the gate, since some checks fail only after their neighbors have run; a failure that shows only in a
  published host, such as one trimming causes, is reproduced by the unfiltered run, which publishes them.
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
- Do make a run's temporary folder, a script's or a build action's, in the repository's ignored folder for a run's temporary files, the build context's temporary folder where the build module is loaded, and remove it when the run ends, so what a run makes for itself stays inside the repository and the clean step reaches what a stopped run left.
- Don't introduce another runtime for tooling; a script needs only PowerShell and the .NET SDK.
