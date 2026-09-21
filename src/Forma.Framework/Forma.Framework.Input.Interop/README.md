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
| `InputScope` | A registration handle. `ActivateAsync` and `ReleaseAsync` move it into and out of the active ordering, `UpdateAsync(profile)` replaces what it contributes, `IsActive` says whether it is held, and `DisposeAsync` removes it from its scope. Each call reports whether it ran, and one the circuit or the script refused is written at `Debug` under the router's category, naming the call and the router's runtime id. |
| `InputInspector` | The inspector behavior over `InputInspector.js`; `AttachAsync(element, owner)` starts reporting to the owner. |
| `AddFormaInputRouter(options)` | Registers the policy through `AddFormaInputServices`, `InputRouter` as a scoped service, and a null logger where the host registered none. |

## Reaching it from a component

`InputRouter` is injected, since one serves the circuit; a component registers its root with a
profile and a `DotNetObjectReference` to itself, exposes `[JSInvokable]`
`HandleInputActionAsync(string action, InputInfo input)` and
`HandleDismissAsync(DismissBehavior action, DismissReason reason)` for what the router delivers,
holds the `InputScope` active while its surface is open, and disposes it from `DisposeAsyncCore`.
`InputInspector` is a behavior wrapper, so a component constructs it from `Context.JS`, attaches it
after the first render, and disposes it with the component.

A component whose root routes one shortcut and stands as a scope for as long as it is rendered:

```csharp
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;

public sealed partial class FormaGallery : FormaElementBase
{
    private static readonly InputProfile Profile = new()
    {
        Routes = [new InputRoute("gallery.select-all", KeyBinding.Shortcut("a"))],
    };

    private DotNetObjectReference<FormaGallery> _selfReference = default!;
    private InputScope? _inputScope;

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    [JSInvokable]
    public Task HandleInputActionAsync(string action, InputInfo input)
        => SelectAllAsync();

    protected override void OnInitialized()
        => _selfReference = DotNetObjectReference.Create(this);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _inputScope = await InputRouter.RegisterAsync(RootElement, Profile, _selfReference);
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        try
        {
            if (_inputScope is { } inputScope)
            {
                _inputScope = null;
                await inputScope.DisposeAsync();
            }
        }
        finally
        {
            _selfReference.Dispose();
        }

        await base.DisposeAsyncCore();
    }
}
```

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
