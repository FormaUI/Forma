# Forma.Examples

Two Web Apps set up as the [library's README](../../README.md#webassembly-components) sets a host up, one for
each culture arrangement, each a server and its WebAssembly client. They reference Forma's packages, never its
projects, so they build as a host outside the repository builds, from what the packages ship.

| Example | What it shows |
| --- | --- |
| `Forma.Examples.Coordinated` | A Server island and a WebAssembly island following one culture, the client starting in the server's |
| `Forma.Examples.Independent` | The same islands, the client's culture its own, switched alone, with culture capture off |

Each page shows the islands' runtime, a string of the host's own from the server's `.locales` folder, which the
client names as its `LocalesPath`, a library component's label from the Hebrew pack, and a switch to Hebrew.

## Build

The gate's browser step packs the solution into `artifacts/browser/packages` at version `1.0.0-examples`, clears
that version from the machine's package cache, publishes each example and drives it in a browser, and holds the
README's setup to the examples' own files. By hand:

```pwsh
dotnet pack Forma.slnx -c Release -o artifacts/browser/packages -p:PackageVersion=1.0.0-examples
dotnet publish tests/Forma.Examples/Forma.Examples.Coordinated -c Release -o artifacts/browser/examples/Coordinated
```

## License

Apache License 2.0, under the repository's [`LICENSE`](../../LICENSE).
