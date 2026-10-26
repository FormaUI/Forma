# Forma Build Implementation Plan

The plan for making the gate quicker to run and easier to follow while it runs, and the record of what has
landed. What the build is and how it is entered is the [repository document](repository-structure.md#build-and-verification)'s.

## Status

Step 1 is under way.

## Steps

1. **Progress and a summary.** The test step reports each test project as it finishes, with how many of
   the projects it has finished, its counts and the time the run has taken, and an estimate of the time
   left from the run before; it closes with the passed, failed and skipped counts across every project and
   the failing tests by name. Every step's heading gives the time it took the last time it passed.
2. **Faster tests.** The slowest test projects and tests are measured and made faster, starting with the
   tests that wait on time and the ones that load the machine.
3. **A faster gate.** The gate's total time comes down, starting with its longest step, the format check.
   The test step builds the solution again although the gate has just built it: the suite takes about
   20 s on the build already on disk and about a minute inside the gate.
