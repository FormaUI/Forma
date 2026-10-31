# Forma.Framework.JavaScript.Interop

The bases Forma's C# wrappers over JavaScript modules are built from, and the scripts that Forma's
other scripts share. A wrapper built on them imports its module on first use, and again on the next
use after an import failed, logs a failed import or call under its owner's logger, and reports a
dropped circuit as a call that did not run.

The package comes with `formaui-net.Blazor`. Its scripts are served from
`_content/forma/core/scripts/`.

## Writing a wrapper

A wrapper takes the `IJSEnvironment` it runs in and names its module's path. A behavior, a module
that attaches to an element for a lifetime, derives from `JSInterop` and exposes a typed
`AttachAsync`:

```csharp
using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;

public sealed class AutofillTracker(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/AutofillTracker.js")
{
    public ValueTask<bool> AttachAsync(ElementReference input)
        => AttachCoreAsync(input);
}
```

An operation, a module of stateless calls, holds a `JSModule` and calls it through `TryInvoke*`:

```csharp
using Forma.Framework.JavaScript.Interop;

public sealed class Clipboard(IJSEnvironment js) : IAsyncDisposable
{
    private readonly JSModule _module = new(js, "./_content/forma/browser/scripts/operations/Clipboard.js");

    public ValueTask<bool> WriteAsync(string text)
        => _module.TryInvokeVoidAsync("write", text);

    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
```

A component builds a wrapper from its `Context.JS`, the environment carrying the component's logger
and `RuntimeId`, attaches it from `OnAfterRenderAsync`, and disposes it from `DisposeAsyncCore`. An
attach that a later attach, a detach or the disposal overtakes releases the instance it gets and
reports that it did not attach, so a component disposed mid-attach leaves nothing attached. A
service builds its own environment from the runtime and logger it is given:

```csharp
_environment = new JSEnvironment(js, logger, Guid.CreateVersion7());
```

| Type | What it is |
| --- | --- |
| `IJSEnvironment` | A JS runtime carrying the `RuntimeId` and `Logger` of the owner whose interop it runs. |
| `JSEnvironment` | An `IJSEnvironment` over an `IJSRuntime`. |
| `JSInterop` | The base of a wrapper whose module attaches to an element: `AttachCoreAsync`, `InvokeAsync` on the attached instance, `InvokeModuleAsync` on the module's exports, `DetachAsync`, `IsAttached`, and disposal as teardown. |
| `JSModule` | A lazily imported module: `InvokeAsync` and `InvokeVoidAsync` throw on failure, and `TryInvokeAsync` and `TryInvokeVoidAsync` return `default` or `false`. |
| `UnavailableJSRuntime` | A runtime whose every call throws `JSRuntimeUnavailableException`, registered where the host has none. |

## Shared scripts

A script in another assembly imports one of these by its path under `_content/forma/core/scripts/`,
as a behavior served from `_content/forma/browser/scripts/behaviors/` does:

```js
import { tell } from '../../../core/scripts/Owner.js';
```

| Script | What it is |
| --- | --- |
| `Controls.js` | The selectors of native controls: what Tab reaches and what handles its own press. |
| `Direction.js` | Whether an element lays out right to left. |
| `FrameScheduler.js` | One piece of work held to the next animation frame, however often it is requested before then. |
| `Owner.js` | `tell(owner, method, ...args)`, which reports to a script's .NET owner. |
| `Sanitizer.js` | `sanitize(html)`, which reduces untrusted markup to what a document may carry. |
| `Scrolling.js` | Which elements scroll, and the nearest ancestor of an element that does. |

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
