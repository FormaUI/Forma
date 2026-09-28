# Forma.Framework.Input.Interop

The browser side of the input router: the script that resolves every input on the page to one
owner, the script that reports its decisions, and the C# that carries a circuit's policy and
registrations to them. A host sets policy through `Forma.Framework.Input` and never reaches this
assembly; a component that owns its root element does.

The assembly is not a package of its own; it ships inside `Forma.Blazor`. It is built on
`Forma.Framework.Input` for the vocabulary and on `Forma.Framework.JavaScript.Interop` for the interop
bases, and its scripts are served under `_content/forma/input/scripts/`.

## Scripts

| Script | Kind | What it does |
| --- | --- | --- |
| `InputRouter.js` | Service | Keeps every scope's routes and the active ordering for the whole document. `register(element, profile, owner, placement)` adds a registration to an element's scope and returns a handle with `activate`, `release`, `update` and `dispose`; `registerHost(profile, owner)` registers the host's routes and preemptive routes on the document element; `configure(options)` sets the host's ambient defaults and binding overrides. `resolve(event)` is the decision itself, and `owns`, `ownerOf`, `describe` and `stack` are views over it; `requestDismiss(element, reason)` offers a platform close request to the foremost active registration. It installs its document listeners on the first registration and removes them after the last, and stamps each active element with its place in the ordering as `data-forma-layer`. |
| `InputInspector.js` | Behavior | Resolves every key, press and wheel turn again without dispatching it and reports the decision and the active ordering to a .NET owner through `ReportAsync`, wheel turns at most once per interval. |

A route with no script handler is delivered to the registration's .NET owner: an action through
`HandleInputActionAsync(action, input)`, the input described by its family so the owner reads it as
the matching `InputInfo` record, and a dismissal through `HandleDismissAsync(action, reason)`.

## Wrappers

| Type | What it is |
| --- | --- |
| `InputRouter` | The circuit's router, a scoped service over `InputRouter.js`. `RegisterAsync(element, profile, owner, parent, within)` registers an element as a scope, or adds to the scope it already is, and answers an `InputScope`, or `null` when the circuit cannot register it. The first registration pushes the host's `InputRouterOptions` through `configure` and, where the options declare routes, registers them through `registerHost` with the router itself as their owner, performing each through the host's `IInputActionHandler`; a push that failed is retried by the next registration. |
| `InputRegistration` | The registration a component keeps on one of its own elements. `SyncAsync(element, profile, wanted, active, parent, within)` is called after every render and brings the registration in step: made while wanted, held active while asked, its profile pushed again when it changed, and released once no longer wanted or the element changed; each sync records what is wanted and one settle at a time carries on until the registration matches the latest, so what changed while a registration was in flight is applied once it lands and one that lands unwanted is released at once; one the router refused is asked for again on the next sync. It owns the reference the router calls back through, forwarding actions and dismissals to the functions it was given, and `DisposeAsync` waits for anything in flight before that reference goes. |
| `InputScope` | A registration handle. `ActivateAsync` and `ReleaseAsync` move it into and out of the active ordering, `UpdateAsync(profile)` replaces what it contributes, `IsActive` says whether it is held, and `DisposeAsync` removes it from its scope. Each call reports whether it ran, and one the circuit or the script refused is written at `Debug` under the router's category, naming the call and the router's runtime id. |
| `InputInspector` | The inspector behavior over `InputInspector.js`; `AttachAsync(element, owner)` starts reporting to the owner. |
| `AddFormaInputRouter(options)` | Registers the policy through `AddFormaInputServices`, `InputRouter` as a scoped service, and a null logger where the host registered none. |

## Reaching it from a component

`InputRouter` is injected, since one serves the circuit; a component keeps an `InputRegistration`
on the element it owns, made with the functions that perform its actions and dismissals, syncs it
after every render with the profile and the hold it wants, and disposes it from `DisposeAsyncCore`.
A component that renders the element itself composes `FormaInputScope` instead, which does the same
for its own root. `InputInspector` is a behavior wrapper, so a component constructs it from
`Context.JS`, attaches it after the first render, and disposes it with the component.

A component whose root routes one shortcut and stands as a scope for as long as it is rendered:

```csharp
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;

public sealed partial class FormaGallery : FormaElementBase
{
    private static readonly InputProfile Profile = new()
    {
        InputRoutes = [new InputRoute("gallery.select-all", KeyBinding.Shortcut("a"))],
    };

    private InputRegistration _registration = default!;

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    protected override void OnInitialized()
        => _registration = new InputRegistration(InputRouter, (action, input) => SelectAllAsync());

    protected override Task OnAfterRenderAsync(bool firstRender)
        => _registration.SyncAsync(RootElement, Profile);

    protected override async ValueTask DisposeAsyncCore()
    {
        await _registration.DisposeAsync();

        await base.DisposeAsyncCore();
    }
}
```

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
