# Forma.Examples

Two Web Apps set up as the [library's README](../README.md#webassembly-components) sets a host up, one for
each culture arrangement, each a server and its WebAssembly client, and three Blazor Hybrid applications set up as
it sets an [application](../README.md#blazor-hybrid) up, one for each host on Windows. They reference Forma's
packages, never its projects, so they build as a host outside the repository builds, from what the packages ship.

| Example | What it shows |
| --- | --- |
| `Forma.Examples.Coordinated` | A Server island and a WebAssembly island following one culture, the client starting in the server's |
| `Forma.Examples.Independent` | The same islands, the client's culture its own, switched alone, with culture capture off |
| `Forma.Examples.Wpf` | A WPF application, registering the platform's web view before Forma, each native window disposing its web view as it closes |
| `Forma.Examples.WinForms` | A Windows Forms application, registering Forma before the platform's web view |
| `Forma.Examples.Maui` | A MAUI application on Windows, Android, iOS and Mac Catalyst, registering on its builder, keeping the culture in its preferences |

Each Web App's page shows the islands' runtime, a string of the client's own from the `.locales` folder beside
its components, a library component's label in Hebrew and its pages turning, and a switch to Hebrew. The
independent client takes that label from the Hebrew pack, and the coordinated client rewords it from its root
`.locales` folder.

Each application's native window shows its web view's runtime, a string of the host's own from the `.locales`
folder its build embeds, the library component's label and its pages turning, a switch to English and to
Hebrew, and, where the platform opens one, another native window to open, its own title in the application's
culture, and the application keeps the culture from one run to the next.

## Build

`Forma.Examples.slnx` lists them all, outside the repository's own solution, and restores once their packages are
packed. Their `nuget.config` restores them into a packages folder of their own, `artifacts/browser/restored`, so the
machine's package cache never holds what the repository packed. Their build makes that folder an `.editorconfig` root,
so a package's own sources compiled into an example, as the MAUI example's are on Windows, take none of the
repository's analyzer rules. The gate's browser step packs the solution into
`artifacts/browser/packages` at the version the root properties file declares, which the examples read from it,
clears that version from the examples' packages folder, publishes each example and drives it, the applications on
Windows alone and through WebView2: its smoke checks start each example and turn a page of the library's
pagination, and the browser check drives each one's culture. The gate's unit tests hold the README's setup to the
examples' own files. The MAUI example builds against the `maui-windows` workload. `scripts/build/mobile.ps1
-Platform` builds it for Android, iOS or Mac Catalyst instead, setting `FormaExamplePlatform` to `android`, `ios`
or `maccatalyst`, against that platform's MAUI workload; Apple's platforms build on a Mac alone. By hand:

```pwsh
dotnet pack Forma.slnx -c Release -o artifacts/browser/packages
Remove-Item artifacts/browser/restored/formaui-net.* -Recurse -Force -ErrorAction Ignore
dotnet publish examples/Forma.Examples.Coordinated -c Release -o artifacts/browser/examples/Coordinated
```

## License

Apache License 2.0, under the repository's [`LICENSE`](../LICENSE).
