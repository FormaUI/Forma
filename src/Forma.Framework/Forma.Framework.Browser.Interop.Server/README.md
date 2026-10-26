# Forma.Framework.Browser.Interop.Server

What the DOM watcher and engine need from an Interactive Server host: when a circuit's connection to
the browser is restored, every observation its components hold is measured afresh and delivered
again in one call, so a state the browser sent while the connection was down still reaches its
component. Nothing is called on the circuit's first connection, before anything has been observed.

The package comes with `formaui-net.Blazor`, and `AddFormaUI` registers it through
`AddFormaBrowserCircuitIntegration`, which also registers the browser services of
`Forma.Framework.Browser.Interop`. A watcher made with no registered bridge is outside its reach and
recovers through its observations' `RefreshAsync`.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
