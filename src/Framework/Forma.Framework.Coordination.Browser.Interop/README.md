# Forma.Framework.Coordination.Browser.Interop

The switch coordinator: the core every coordinator of a setting the whole page shares is built on, as the culture's is.
It orders the setting's switches across every runtime on the page, each Interactive Server circuit and WebAssembly
runtime, numbers each in the document's sequence, and delivers the accepted value to every runtime registered with it,
so they converge on one value. The
[culture design](https://github.com/FormaUI/Forma/blob/main/docs/culture-design.md#the-coordinator) sets out the rules
it holds.

## Install

The package comes with the coordinators built on it, such as `formaui-net.Framework.Culture.Browser.Interop`, so a host
references it directly only to build a coordinator of its own.

```pwsh
dotnet add package formaui-net.Framework.Coordination.Browser.Interop --prerelease
```

## In the browser

`scripts/SwitchCoordinator.js`, served at `_content/forma/coordination/scripts/SwitchCoordinator.js`, exports two
classes a setting's coordinator script builds on:

| Class | What it holds |
| --- | --- |
| `SwitchCoordinator` | One setting's accepted value with its number in the document's sequence, every runtime's registration, the queue that accepts requests one at a time, and the hold through an enhanced navigation, after which it reads the landing and sets the accepted value again |
| `SwitchRegistration` | One runtime state's registration: its runtime, its receiver, and the place of each request and landing it reports, so a late one is refused |

```js
import { SwitchCoordinator, SwitchRegistration } from '../../coordination/scripts/SwitchCoordinator.js';

const coordinator = new SwitchCoordinator({ write: applyToDocument, toArguments: value => [value.name] });
```

A coordinator `start`s from the value the page started in, `accept`s each switch as the next number, and delivers it to
each registration's receiver as `ApplyAsync(...arguments, documentKey, number)`. `enqueue` runs a request once every
request before it settled and no navigation holds the document, and `track` follows Blazor's enhanced page updates.

## In .NET

`SwitchCoordinatorRegistration` is the base of a user scope's registration with a coordinator. It makes each attempt to
register through the coordinator's module, marks the registration unavailable as the connection to the browser goes
down, so a switch fails at once until it registers again, and releases the browser's registration, its receiver and
its module as it's disposed. A setting's registration derives from it, as `CultureCoordinatorRegistration` does, and
adds its own requests and its receiver's deliveries.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
