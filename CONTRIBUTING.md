# Contributing to Forma

Forma takes feature and fix pull requests against `dev`.

1. Branch from `dev`: `git switch --create feature/my-change origin/dev`.
2. Run `./build.ps1 Fast` while you work, and `./build.ps1`, the full gate, before you open the pull request.
3. Open the pull request [against `dev`](https://github.com/FormaUI/Forma/compare/dev...). The fast gate is its
   required check.

`main` holds the commit approved for release and receives it from `preview` alone, so a pull request into `main`
from any other branch is blocked until its base is changed to `dev`.

[The repository's structure](docs/repository-structure.md) sets out the build and the checks, and
[the verification design](docs/verification-design.md#branches-and-promotion) how a change reaches a release.
