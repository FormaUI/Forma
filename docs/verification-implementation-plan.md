# Forma Verification Implementation Plan

The steps that carry out the [verification design](verification-design.md)'s nightly hunt for intermittent
failures, its gate policy, and the release command a promotion ends with. Each step is built, reviewed and
committed on its own, and later steps rest on earlier ones.

## Why these steps

A release's checks surfaced failures nobody had seen, each costing an investigation while the release waited:

- A web view check failed 5 to 7 runs in 10 when its neighbors ran first, and earlier gates had passed it by
  chance.
- The nightly run failed two other checks that pass most of the time, and nothing reported them.
- The Android emulator went offline during a candidate's run.

The full gate also ran locally twice and then again in CI, so a release spent about 75 minutes verifying the same
commit. And the push that publishes a release stopped at a manual step.

## Steps

1. ✅ **The browser step repeats, filters and reports.** `browser.ps1` takes `-Filter`, with the filter syntax
   `test.ps1` takes, `-Times` and `-LoadWith`, as `test.ps1` does. Each run writes a results file. The step
   finishes every run it was asked for, ends with each failing check and how many of its runs failed it, and
   fails if any check failed. A filter that selects no check fails the step. Gate: the build's own cases, and a
   filtered, repeated run of a known intermittent check that reports its rate.
2. ✅ **The Android harness says when the device is unreachable.** A launch adb refuses because the device is offline
   throws an error of its own, carrying the device's state, so a report counts the checks it interrupted as not
   run rather than failed. A job whose emulator went unreachable describes it: its process, the runner's memory, what
   the kernel killed and its log. Gate: the support assembly has no test project of its own, so a call through the
   harness with no device attached was made instead, which threw the error with adb's device list, and the build's
   rate case counts an interrupted check apart.
3. ✅ **The nightly run checks one commit, three times.** The nightly run resolves `dev` to one SHA first, and every
   job checks out that SHA. The browser step runs three times on Linux and on Windows: once in each gate, and
   twice more in jobs of their own. Every run's results are kept. The design's gate table says so. Gate: a
   dispatched nightly run on `dev`.
4. ✅ **The nightly run files what failed.** A last job reads every run's results and opens or updates one issue per
   failing check, recording the SHA, the platform, the runtime, the first error and the run. A check that passed
   and failed on one SHA is labeled `flaky`. One that failed every run on a SHA where it had passed before is
   labeled a regression. An unreachable device gets one infrastructure issue, its interrupted checks listed as not
   run. Gate: the report's cases on results made for them, and the dispatched run of step 3.
5. ✅ **The known intermittent failures are fixed at their cause.** Each is reproduced on the SHA it failed on, and
   again on the fix:
   - ✅ `WindowsTests+RestArea.Should_restore_a_minimized_window_from_its_handle`, on WebAssembly in the Linux gate,
     which failed once before in Android's web view under Auto with WebAssembly. The activation a window's opening
     asks for, decided while it was shown, could apply after the check's minimize, and an activation restores what it
     activates. Delaying that activation past the minimize restored the window in every run before the fix and in
     none after it. The window's opening, a press and the focus arriving now activate it only while it is visible;
   - ✅ `WebViewProbeTests.Should_tell_a_traversal_from_a_navigation_the_web_view_makes`, on Windows. The switch its
     navigation to another culture's address made was followed in the web view as Back began, and Blazor
     superseded that follow with the traversal, a navigation the check didn't expect. A follow held until the
     traversal began failed it every run. The check now navigates within one culture, and the culture check that
     traverses after a switch waits for the switch's follow to land first;
   - ✅ the Android emulator going offline in the MAUI example's job. Stopping the share sheet's package while its
     window showed crashed the emulator, which exited on a segmentation fault: repeating the share checks with that
     stop failed 8 of 10 jobs, 7 of them on the crash, and dismissing the sheet with Back before the stop failed
     none. The harness now dismisses it with Back first, and 160 runs of the share checks over 10 jobs then kept the
     emulator up. The same runs showed a refused connection to the web view's forwarded port, which the harness
     chose and adb then bound, so adb now chooses the port itself.
6. ✅ **The local gate follows the change.** Once steps 1 to 5 have been exercised, the design states when each
   gate runs: before a commit, the fast gate, the format check where C# changed, and the browser checks for what
   the change touches; the full gate in CI, nightly on `dev` and on every push to `candidate`; and locally for a
   change to shared interop, the component bases, the hosts or the build, and to reproduce a CI failure. The
   verifying skill, the working rules and the repository guide point to the design rather than restating it.
   It landed once the nightly hunt, steps 1 to 4, had run on `dev`, the condition the maintainer set. The contributing
   guide points to the design too, and the full workflow's header no longer calls the full gate the one before a
   commit.
7. ✅ **The release command.** Versioning ships one command a maintainer runs after asking for a release. It checks
   the repository, the promotion pull request and the passing runs for the candidate's exact SHA, and holds the
   release to the release check before anything is pushed. It signs the tag locally and verifies the signature,
   then pushes `main` and the tag in one atomic push, and resumes a release that stopped partway. It runs from a
   checkout of a released Versioning tag outside any working tree, which one standing permission names. Gate:
   its cases, and Versioning's own release made through it.
8. ✅ **Forma `1.0.0-preview.4` is published** through the release command, and checked from an empty package
   cache.
