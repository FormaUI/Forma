# Forma Verification Implementation Plan

The plan for bringing the repository to [the verification design](verification-design.md): its steps, the
gate each passes, and their state.

## Steps

Each step lands buildable and passes its gate before the next starts.

1. **The move ledger.** Each suite's current and proposed path, assembly name, kind, runtime and gate. A
   project mixing kinds is classified by its assertions. `Forma.Analyzers.Tests` moving under `eng/`, the
   JavaScript unit tests, which have no project file, and the Android example driver, apart from the 149
   Android component rows, are each accounted for. Gate: the maintainer's review of the ledger.
2. **The moves.** Projects move to their places with every reference to their paths: `Forma.slnx`, the build
   and mobile scripts, CI's path filters, the README's example markers, the repository's guides and the
   examples' build and NuGet configuration. Production assembly names and static asset paths stay as they are.
   Gate: the full gate.
3. **The splits and the support.** Mixed suites split by kind and helpers move by the support rule, test
   discovery compared after each split. The browser check's 926 checks outside Android and its 149 Android
   rows are the starting inventory, which records the checks that move to smoke, so a suite that passes
   because it silently stopped discovering part of itself is caught. The testing guide and the repository's
   working rules take the naming and support rules as they land. Gate: the full gate, discovery matching the
   inventory.
4. **The fast gate.** `./build.ps1 Fast`, its selection verified to run on Linux and timed from a job's start
   to its result, setup and restore included; `./build.ps1` stays the full local gate. The step timings decide
   any further work, such as running the JavaScript and .NET tests side by side. Gate: the selection's
   discovery and its timing.
5. **The promotion workflow.** CI runs the fast gate on feature branches and `dev`, the full gate nightly on
   `dev` and on each `preview` promotion, and the release check on an annotated tag. Android's E2E is confirmed
   in both full workflows before the weekly Android workflow retires, and the release check is exercised with a
   passing `preview` commit and with a later commit yet to pass, so only the verified commit is eligible for
   promotion and publication. Gate: those runs.
