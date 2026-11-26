# Troubleshooting the build

What to do when the machine, rather than the change, stops a build or a check.

## A file the build can't copy

- A running showcase holds its `bin` DLLs open, so a build copying over them fails with `MSB3027`. The showcase
  script stops the showcase listening on its port before it builds, and refuses a port held by anything but this
  repository's showcase rather than stopping it.
- MSBuild keeps its worker nodes alive between builds, and they keep a handle on what they last built, so a copy can
  fail with nothing appearing to use the file. Stop them through the repository's script for them. The broader
  script beside it also stops whatever is running out of this repository, such as the showcase. The language server
  an editor runs for C# stays running under both, unless the broader one is asked to stop every .NET process on the
  machine.

## A solution the editor rewrote

The C# extension rewrites the solution file after a project is added, with a duplicate `<Folder>` entry and without
`<Configurations>`, which then fails the build with `MSB4025`. Reopen the solution file after adding a project and
undo the rewrite.

## A stale showcase

The showcase runs a compiled binary with no hot reload. To confirm the running one contains a change, check that the
build succeeded and that the library's DLL in the showcase's output is newer than the sources changed; if it isn't,
the build never reached the app.

## A filling disk

A task program run from a single `.cs` file is built into the system temp directory and left there on every edit and
rename. Clean the task programs' build output too, with the clean step's widest sweep.
