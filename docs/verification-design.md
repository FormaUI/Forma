# Forma Verification Design

Forma's code is checked by tests of five kinds, run by two gates. This design states what each kind proves,
where its suites live and what they are named, where the helpers they share live, which gate runs what and
when, and how a commit is promoted from the integrated work to a release.

## Kinds of test

Every test is one of five kinds, and its kind decides where it lives, what its suite is named and which gate
runs it. A check is classified by what must work for its assertion to pass, rather than by what it uses: a
detailed culture transition belongs in E2E even when it runs through an example host, comparing a README's text
with an example's is a unit check though it reads both from disk, and verifying that a Forma operation created
the expected file on disk is an integration check.

- **Unit.** Every test of no other kind: an isolated behavior exercised in process on ordinary inputs, fakes
  optional.
- **bUnit.** A component rendered by bUnit and driven through its markup.
- **Integration.** Several parts tested together through real things, such as a file system whose state the
  test verifies, short of a complete host running end to end.
- **Smoke.** A complete host runs and briefly works: a consumer builds an example from Forma's packed packages,
  starts it and uses a Forma component. On an interactive host a smoke check performs one action and verifies
  its visible result, which catches a host that renders its first screen but never becomes interactive.
- **E2E.** A complete host not only runs but behaves correctly end to end: each component's interactions in
  every runtime, a culture's transitions, the download a client costs.

## Layout

```text
eng/
examples/
src/
  AddOns/
  Blazor/
  Framework/
  Locales/
tests/
  bunit/
  e2e/
  integration/
  scripts/
  smoke/
  support/
  unit/
    AddOns/
    Blazor/
    Examples/
    Framework/
```

- `examples/` holds the consumer examples, built from Forma's packed packages under build and package
  configuration of their own.
- `src/` groups the product by what it is. `Forma.Blazor.Showcase`, Forma's maintained component gallery,
  stays in `Blazor/`.
- `tests/unit/`, `tests/bunit/` and `tests/integration/` repeat `src/`'s grouping wherever there are suites to
  put in it, so the bUnit tests of a Framework component live in `tests/bunit/Framework/`, and the unit tests
  of the examples sit in `Examples/`.
- `tests/e2e/` groups by scenario: a component's check pages and the hosts that render them sit together, as
  do the minimal and full clients a download budget measures, so whoever investigates a failure finds what it
  ran on beside it.
- `tests/scripts/` holds the unit tests of the JavaScript Forma ships, its test files grouped by the assembly
  whose modules they cover.
- `tests/support/` holds the shared support projects.
- `eng/` holds the analyzers' tests beside the analyzers, and `scripts/` the task programs' tests beside the
  programs: a single-file test program that names its packages in its own header and includes the code it
  tests.

## Names

There is one test project per production assembly and kind, where that pairing exists:

- `X.Tests` holds the unit tests of `X`.
- `X.BUnit.Tests` holds its bUnit tests.
- `X.Integration.Tests` holds its integration tests.

A suite that exercises a complete host or spans several assemblies is named after the scenario it verifies,
as `Forma.Examples.Smoke.Tests`. Every test assembly's name is unique, so a failure reported in CI or an IDE
names its kind as well as its subject, and `InternalsVisibleTo` grants a production assembly's internals to
exactly the suites that need them. A production assembly's name and its static asset paths don't follow the
layout.

## Support

A helper lives in its owning project's `Support/` folder until a second project needs the same behavior, and
then moves to the shared support project: two genuine consumers are enough. The rule applies per type, since
two projects importing one support namespace may use entirely different helpers. A helper that needs bUnit
lives in the bUnit support project, so a plain suite keeps its smaller dependency set, and a Framework
component's tests use bUnit while the Framework's plain tests keep their isolation.

Smoke and E2E keep fixture lifetimes and assertions of their own and share the operations a host's lifecycle
needs: publishing it, launching it, waiting until it is ready, stopping it and cleaning up after a start that
failed, so a fix to a host's startup reaches both.

## Gates

- **Fast,** `./build.ps1 Fast`: every unit and bUnit test, the JavaScript unit tests and the task programs'
  tests. It restores and builds
  the selected projects and their dependencies once, analyzers on, then runs the tests without rebuilding,
  leaving out the example hosts, the browser hosts, packaging and the solution's unrelated projects. It runs on
  ordinary feature pushes, on pull requests into `dev` and on integrated `dev` commits, and returns feedback in
  a few minutes: two where practical, five or six acceptable, and a routine push rarely tens. A selected suite
  that needs another platform than Linux runs in a job that can run it.
- **Full,** `./build.ps1`: everything, packaging, integration, smoke, the browsers, WebView2 and Android's E2E
  among it. It runs every check the machine it runs on can execute, naming each platform check it couldn't, as
  WebView2's away from Windows or Android's without a device. A full run in CI passes only once every required
  platform job has passed for the same SHA: Linux's, Windows' with WebView2, and Android's E2E. The MAUI example's
  builds for iOS and Mac Catalyst, on a hosted Mac, are reported beside them and hold nothing. The nightly run on
  `dev` holds the same coverage as a candidate's.

**A change runs the gates its reach calls for.** Before a commit, it runs the fast gate, the format check where C#
changed, and the browser checks for what it touches, `scripts/build/browser.ps1 -Filter`. While it's made, a
browser check runs in one runtime, `Runtime=server` among the filter's terms, and in every runtime once the change
is otherwise done; a change split into commits runs the fast gate on each, and the browser checks and the format
check once, on its final state. It runs the full gate
locally where it changes what every component rests on, shared interop, the component bases, the hosts or the
build, and where a failure CI found is being reproduced. Everything else the full gate covers runs in CI, nightly on
`dev` and on every push to `candidate`, and the nightly run files what fails, so an intermittent failure or a
regression a narrower run missed comes back as an issue before a candidate is selected.

## Branches and promotion

Forma follows [Versioning `1.0.0-preview.5`](https://github.com/FormaUI/Versioning/blob/v1.0.0-preview.5/README.md): its [branches](https://github.com/FormaUI/Versioning/blob/v1.0.0-preview.5/docs/branches.md)
set out how work lands on `dev` and how a candidate is stabilized on `candidate`, and its
[releases](https://github.com/FormaUI/Versioning/blob/v1.0.0-preview.5/docs/releases.md) how `main` receives the candidate by promotion and how its tag is
released. What Forma adds is when each of its gates runs. The nightly run, which GitHub schedules from the default
branch's workflow, resolves `dev` to one commit, which every job checks out, and a run dispatched by hand is a nightly
run of the branch it's dispatched on.

| Event | What runs |
| --- | --- |
| A push to `dev` or to a fix or feature branch, and a pull request into `dev` | The fast gate, required of every pull request into `dev`; a feature's branch, its pull request and the integrated `dev` commit are each checked once. |
| Nightly on `dev` | The full gate, Android's E2E included, so an integration regression surfaces before a candidate is selected; and the browser check twice more on Linux and on Windows, each run on a runner of its own, so a check that fails sometimes shows as a rate. Every job keeps its checks' results. |
| A push to `candidate` | The full gate on the candidate's SHA, packaging, integration, smoke, the browsers, WebView2 and Android's E2E included; its Linux gate keeps the packages it packed, built the examples from and checked. |
| A pull request into `main` | Versioning's promotion check, the required check `promotion`. |
| A release tag | Versioning's release check, then the candidate's packages attached to the tag's GitHub release and published from there to nuget.org and the organization's GitHub feed. |

**The nightly run files what fails.** A last job reads every run's results together and opens or updates one issue
for each failing check, labeled `nightly`. It's labeled `flaky` where the check passed in some runs of the commit and
failed in others, and `regression` where it failed every run that ran it, unless an earlier night showed it
intermittent. Each later night it fails comments on the issue with the commit, the machine, the runtime, its rate, its
first error and the run. A check an unreachable Android device interrupted counts as not run, and the night's
interrupted checks make one issue labeled `infrastructure`, as does a job that failed with no failing check of its own.
