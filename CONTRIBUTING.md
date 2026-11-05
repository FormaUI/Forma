# Contributing to Forma

Forma takes fix and feature pull requests against `dev`, from a branch named `fix/<name>` for a correction of behavior
that's wrong and `feature/<name>` for every other change.

1. Branch from `dev`: `git switch --create feature/my-change origin/dev`.
2. Run `./build.ps1 Fast` while you work, and `./build.ps1`, the full gate, before you open the pull request.
3. Open the pull request [against `dev`](https://github.com/FormaUI/Forma/compare/dev...). The fast gate is its
   required check. It lands by fast-forward: the branch is rebased onto `dev` and `dev` advances to it, so every
   commit keeps its signature and `dev`'s history stays linear.

Every commit on `dev`, `candidate` and `main` carries a verified signature. `main` holds the latest release and
receives it from `candidate` alone, so a pull request into `main` from any other branch fails its promotion check
until its base is changed to `dev`.

[Versioning](https://github.com/FormaUI/Versioning/blob/v1.0.0-preview.3/README.md) sets out the rules every FormaUI project follows for its branches, commits and
releases, [the repository's structure](docs/repository-structure.md) the build and the checks, and
[the verification design](docs/verification-design.md#branches-and-promotion) when each check runs.
