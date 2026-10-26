# Forma.Framework.Input.Interop

The input router: the script that resolves every key, press, wheel turn and drop on the page to one
owner, and the C# a component registers its elements through. It carries the policy and routes
described in `Forma.Framework.Input` to each circuit's document and delivers the actions and
dismissals it resolves back to their owners.

The package comes with `formaui-net.Blazor`, and `AddFormaUI` registers the router through
`AddFormaInputRouter(options)`: the policy as a singleton `InputRouterOptions` and `InputRouter` as a
scoped service, one per circuit. Its scripts are served from `_content/forma/input/scripts/`.

## Registering a component's element

A component injects `InputRouter`, keeps an `InputRegistration` for the element it owns, syncs it
after every render and disposes it with the component. A search box that runs its search on Enter:

```csharp
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;

public sealed partial class FormaSearchBox : FormaElementBase
{
    private const string SearchAction = "search-box.search";

    private static readonly InputProfile SearchProfile = new()
    {
        InputRoutes = [new InputRoute(SearchAction, new KeyBinding("Enter"))],
    };

    private InputRegistration _inputRegistration = default!;

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    protected override void OnInitialized()
        => _inputRegistration = new InputRegistration(InputRouter, HandleActionAsync);

    protected override Task OnAfterRenderAsync(bool firstRender)
        => _inputRegistration.SyncAsync(RootElement, SearchProfile);

    private Task HandleActionAsync(string action, InputInfo input)
        => action == SearchAction ? SearchAsync() : Task.CompletedTask;

    protected override async ValueTask DisposeAsyncCore()
    {
        await _inputRegistration.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
```

A surface that opens and closes passes its state on each sync, so the element is registered and
held foremost only while it is open:

```csharp
await _inputRegistration.SyncAsync(RootElement, _inputProfile, wanted: IsOpen, active: IsOpen, within: [_trigger]);
```

Markup that needs routes around content it renders composes `FormaInputScope`, a component that
registers its own element the same way.

## The surface

| Member | What it is |
| --- | --- |
| `InputRegistration(router, onAction, onDismiss)` | A registration a component holds on one of its elements, performing actions and dismissals through the functions it is given. |
| `InputRegistration.SyncAsync(element, profile, wanted, active, parent, within)` | Brings the registration in step: the element registered with the profile while `wanted`, held foremost while `active`, inside `parent`, with the `within` elements, such as the trigger that opened it, counted as inside it. |
| `InputRegistration.IsRegistered`, `IsActive` | Whether the element is registered, and whether it is held active. |
| `InputRouter.RegisterAsync(element, profile, owner, parent, within)` | Registers an element directly and returns its `InputScope`, whose `ActivateAsync`, `ReleaseAsync`, `UpdateAsync(profile)` and `DisposeAsync` drive it by hand. |
| `InputInspector.AttachAsync(element, owner)` | A behavior built from `Context.JS` that reports how the router resolves each key, press and wheel turn to the owner's `ReportAsync`, for a debugging surface. |

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
