# Forma.Framework.Input

The vocabulary of input routing: the routes a component declares on the element it owns, the
bindings that raise them, the profile that says how the scope treats Escape and a press away, the
policy a host sets for the whole application, and the records an owner reads an input back as. One
router per page resolves every key, press, wheel turn and drop to one owner, so a component
implements its actions and never arbitrates against the components around it.

The assembly is not a package of its own; it ships inside `2Site.Forma.Blazor`, and `AddFormaUI`
registers its policy. Its types live in `Forma.Framework.Input`, with a binding and an input
record per family under `.Keyboard`, `.Pointer`, `.Wheel` and `.Drop`.

## How input is routed

A component registers the element it owns as a **scope**, with a profile of **routes**. Each route
names a semantic **action**, `{owner}.{action}`, and the **bindings** that raise it by default; the
host remaps bindings by action, and the owner receives the action. A scope is **active** while its
surface is open — a popup, a dialog, a window — and **modal** when nothing activated beneath it may
answer.

Targeted input — a key, a wheel turn, a press, a drop — walks the event's composed path from the
target outward and belongs to the first scope whose route matches. The walk stops short at a native
control that consumes the input itself, at any active or modal scope, and at a scope that declares
itself a boundary for that event type; input stopped there is left to the browser, and no scope
further out acts on it. A scope on a control is asked before the control's native claim, so a route
registered on an editable element takes a shortcut the control would otherwise keep.

Ambient input — Escape, a press away, a platform close request — belongs to the foremost active
scope alone, which dismisses, cancels, or ignores it as its profile says; nothing beneath is offered
the same input. Foremost is decided by the modal barrier, then parentage, then activation order,
then registration order.

A host's own routes stand outside that walk: its `Routes` are answered once every scope on the path
has declined, and its `PreemptiveRoutes` before anything local, ambient input excepted.

## Routes

| Member | What it is |
| --- | --- |
| `InputRoute(action, bindings)` | Declares the action, raised by the bindings by default. The action is `{owner}.{action}` in lowercase letters, digits and hyphens, such as `composer.bold`; an unqualified name is rejected. |
| `Action` | The action's name, which the host remaps bindings by and the owner receives. |
| `Bindings` | The inputs that raise the action by default. |
| `Priority` | Decides between routes whose bindings match equally well; higher wins. |
| `PreventDefault` | Whether the browser's default is prevented once the route consumes the input; `null` takes the input class's default. |
| `Propagation` | Whether the input continues past the scope, `InputPropagation.Continue`, or stops at it, `Stop`; `null` takes the input class's default. |
| `InputRoute.Qualify(owner, name)` | Qualifies a name under an owner, lowered and slugged, so `Insert Table` under `toolbar` is `toolbar.insert-table`. |

When several bindings on one scope match, the most specific wins — the one stating the most fields
and modifiers — so a plain ArrowDown never takes Shift+ArrowDown from the binding that names Shift.
Among equally specific bindings `Priority` decides, and among equal priorities registration order
does.

A consumed input takes the default of its class unless the route says otherwise:

| Input | Default prevented | Propagation |
| --- | --- | --- |
| `keydown` | Yes | Stops |
| `keyup` | No | Stops |
| `wheel` | Yes | Stops |
| `pointerdown`, `pointerup`, `pointermove` | No | Continues |
| `drop`, `dragover` | Yes | Continues |

## Bindings

Every binding derives from `InputBinding` and is matched by the rules of its family.

| Type | What it matches | Presets |
| --- | --- | --- |
| `KeyBinding(key)` | The key, compared without regard to case, on `keydown` or, with `Released`, on `keyup`. Control, Alt and Meta must be absent unless `Modifiers` states them; Shift matches either way unless stated. | `KeyBinding.Shortcut(key, shift)`, the key with the platform's primary modifier |
| `PointerBinding` | A `pointerdown`, or with `Phase` a `pointerup` or `pointermove`, by the `Button` that changed, the `Buttons` held, and the `PointerType`; only the modifiers it states are checked, and a field left `null` matches any. | `Down`, any press; `Drag`, a move with the primary button held |
| `WheelBinding` | A `wheel` turn by the `Axis` it mostly moves along; only the modifiers it states are checked. | `Any` |
| `DropBinding` | A `drop`, or with `Over` a `dragover`, carrying any of the `Data` kinds as the drag's data transfer names them; `null` matches any. | `Any`; `Files` |

`ModifierRequirement` states the modifiers a binding needs: `Control`, `Alt`, `Shift` and `Meta`
each `true`, `false`, or `null` for the family's default, and `Primary` for the platform's shortcut
modifier, Command on Apple platforms and Control elsewhere. `ModifierRequirement.None` states
nothing and `ModifierRequirement.Shortcut` states `Primary`. A binding cannot require `Primary` and
require that same key absent.

A key pressed during composition, through an input method editor or a dead key, is routed to
nothing.

## Scopes

An `InputProfile` is what one registration contributes to a scope. Several registrations on one
element share its scope; the scope's routes are their union, and it is active while any of them
holds it.

| Member | What it is |
| --- | --- |
| `Modal` | Whether the scope is a barrier that nothing activated beneath it can answer through. |
| `Escape` | What the scope does with Escape while foremost; `null` inherits the host default. |
| `CloseRequest` | What the scope does with a platform close request while foremost; `null` inherits the host default, and failing that does what Escape does. |
| `PressAway` | How the scope responds when pressed away from while foremost; `null` inherits the host default. |
| `Boundary` | The event types the scope stops from reaching the scopes around it when no route of its own matches. |
| `Routes` | The inputs the registration answers. |
| `InputProfile.Default` | A registration with no routes that inherits the host's ambient defaults. |

Escape and a close request take a `DismissBehavior`: `Dismiss`, `Cancel`, or `Ignore`, which
leaves the input alone without offering it to a scope beneath. A press away takes a
`PressAwayBehavior`: `Dismiss`, which swallows the click that follows the press so nothing else
acts on it; `PassThrough`, which dismisses the scope and lets the press reach whatever was pressed;
or `Ignore`. A dismissal reaches its owner as the behavior declared, `Dismiss` or `Cancel`, with
the `DismissReason` that raised it: `Escape`, `PressAway`, or `CloseRequest`.

Native editing controls keep the input they consume even inside a scope that routes it, so a
grid's arrow routes never take the caret keys from a text field in one of its cells:

| Control | Consumes |
| --- | --- |
| `input`, text-like types | Typing, Home, End, ArrowLeft, ArrowRight, Enter, Backspace, Delete, and Control or Meta with A, C, V, X, Z or Y |
| `input[type=number]` | The above, ArrowUp and ArrowDown, and wheel while focused |
| `textarea` | The text-like keys, ArrowUp, ArrowDown, PageUp, PageDown, and wheel while it scrolls |
| `input`, date and time types | Typing, the arrows, Home, End, Enter, Backspace, Delete |
| `input[type=range]` | The arrows, Home, End, PageUp, PageDown |
| `input[type=radio]` | The arrows and Space |
| `input[type=checkbox]` | Space |
| `select` | Typing, the arrows, Home, End, PageUp, PageDown, Enter, Space |
| `button`, button-like `input` types, `a[href]`, `summary`, `[role=button]`, `[role=link]` | Enter and Space |
| `[contenteditable]` | Everything a `textarea` consumes, and Control or Meta with B, I or U |

A disabled control consumes nothing. Input a control does not consume — Escape, an unrelated
shortcut, wheel on a control that does not scroll — resolves through the enclosing scopes as usual.

## Host policy

`InputRouterOptions` holds the application-wide policy, given to `AddFormaUI`.

| Member | What it is |
| --- | --- |
| `Escape` | What a scope does with Escape unless its profile says otherwise. Defaults to `Dismiss`. |
| `CloseRequest` | What a scope does with a platform close request unless its profile says otherwise; `null` makes it do what Escape does. |
| `PressAway` | How a scope responds when pressed away from, unless its profile says otherwise. Defaults to `Dismiss`. |
| `Routes` | The routes the host answers once every component scope has declined. |
| `PreemptiveRoutes` | The routes the host answers before any component scope, native control or boundary is consulted; only Escape and a press away come first. |
| `Bindings` | The bindings the host changes, keyed by action; an action absent from the map keeps its defaults. |
| `InputRouterOptions.Default` | Escape and a press away dismiss, a close request does what Escape does, the host answers no route, and every action keeps its default bindings. |

An `InputBindingOverride` changes one action's bindings: `Replace` stands in for the defaults
outright, or, left `null`, `Add` and `Remove` adjust them, a removed binding matched by its every
field. `InputBindingOverride.ReplaceWith(bindings)` and `InputBindingOverride.Unbound` cover the two
common cases.

A host that declares routes registers an `IInputActionHandler`, whose `HandleAsync(action, input)`
performs each host action for the input that raised it. Routes without a handler are refused.

`AddFormaInputServices(options)` is the assembly's own registrar: it registers the policy as a
singleton `InputRouterOptions`, or `InputRouterOptions.Default` when given none, and leaves a
policy the host registered first in place. `AddFormaUI` calls it with the options it is given.

## What an owner receives

An action arrives with the input that raised it, as an `InputInfo` of the matching family. Every
input carries its DOM event `Type`.

| Type | What it carries |
| --- | --- |
| `InputKeyInfo` | `Key`, the physical `Code`, and the `Modifiers` held. |
| `InputPointerInfo` | `PointerId`, `PointerType`, the `Button` that changed, the `Buttons` held, `ClientX`, `ClientY`, and the `Modifiers` held. |
| `InputWheelInfo` | `DeltaX`, `DeltaY`, `DeltaMode`, and the `Modifiers` held. |
| `InputDropInfo` | The `Data` kinds the drag carries, `ClientX` and `ClientY`. |

`ModifierKeys` says which of `Control`, `Alt`, `Shift` and `Meta` were held, and `Primary` whether
the platform's shortcut modifier was.

## Declaring a scope

`FormaInputScope`, in `Forma.Blazor.Components.Interaction`, registers its element as a scope from
markup: `Profile` is what it contributes, `Active` puts it in the active ordering, `Parent` names
the scope it stands inside in place of the nearest scope containing it, `Within` lists elements outside
it that count as inside, and `OnAction` and `OnDismiss` deliver what the router resolves to it, as
an `InputAction` and an `InputDismissal`.

A picker whose list answers the arrows, Home, End and Enter while open, dismisses on Escape, and
closes on a press away while letting that press act on what it landed on; its trigger is counted
inside, so a press there toggles rather than dismisses:

```razor
@using Forma.Blazor.Components.Interaction
@using Forma.Framework.Input
@using Forma.Framework.Input.Keyboard

<button type="button" @ref="_trigger" @onclick="() => _open = !_open">@_members[_index]</button>

<FormaInputScope Profile="ListProfile" Active="_open" Within="Within" OnAction="HandleAction" OnDismiss="_ => _open = false">
    @if (_open)
    {
        <ul role="listbox">
            @foreach (var member in _members)
            {
                <li role="option" aria-selected="@(member == _members[_index])">@member</li>
            }
        </ul>
    }
</FormaInputScope>

@code {
    private static readonly InputProfile ListProfile = new()
    {
        Escape = DismissBehavior.Dismiss,
        PressAway = PressAwayBehavior.PassThrough,
        Routes =
        [
            new InputRoute("assignee.next", new KeyBinding("ArrowDown")),
            new InputRoute("assignee.previous", new KeyBinding("ArrowUp")),
            new InputRoute("assignee.first", new KeyBinding("Home")),
            new InputRoute("assignee.last", new KeyBinding("End")),
            new InputRoute("assignee.choose", new KeyBinding("Enter")),
        ],
    };

    private readonly string[] _members = ["Ada", "Grace", "Linus"];
    private ElementReference _trigger;
    private bool _open;
    private int _index;

    private IReadOnlyList<ElementReference> Within
        => [_trigger];

    private void HandleAction(InputAction action)
    {
        switch (action.Action)
        {
            case "assignee.next":
                _index = Math.Min(_index + 1, _members.Length - 1);
                break;
            case "assignee.previous":
                _index = Math.Max(_index - 1, 0);
                break;
            case "assignee.first":
                _index = 0;
                break;
            case "assignee.last":
                _index = _members.Length - 1;
                break;
            case "assignee.choose":
                _open = false;
                break;
        }
    }
}
```

While the picker is open it is active, so every key pressed inside it stays inside it: its own
routes answer the arrows, and a key it does not route stops at it rather than reaching a grid or a
host route around it. A library component that owns its root element registers it through
`InputRouter` in `Forma.Framework.Input.Interop` instead of composing `FormaInputScope`.

## Registering host routes

A palette shortcut that nothing on the page may keep, a help key that reaches the host only when no
scope claims it, and two remapped actions:

```csharp
using Forma.Blazor;
using Forma.Framework.Input;
using Forma.Framework.Input.Keyboard;

builder.Services.AddFormaUI(new InputRouterOptions
{
    PreemptiveRoutes = [new InputRoute("app.palette", KeyBinding.Shortcut("p", shift: true))],
    Routes = [new InputRoute("app.help", new KeyBinding("F1"))],
    Bindings = new Dictionary<string, InputBindingOverride>
    {
        ["toolbar.bold"] = InputBindingOverride.ReplaceWith(KeyBinding.Shortcut("b"), new KeyBinding("F6")),
        ["assignee.next"] = new() { Add = [new KeyBinding("j")] },
    },
});
builder.Services.AddScoped<IInputActionHandler, AppShortcuts>();
```

The handler that performs them:

```csharp
using Forma.Framework.Input;

public sealed class AppShortcuts(CommandPalette palette, HelpCenter help) : IInputActionHandler
{
    public Task HandleAsync(string action, InputInfo input)
        => action switch
        {
            "app.palette" => palette.OpenAsync(),
            "app.help" => help.ShowAsync(),
            _ => Task.CompletedTask,
        };
}
```

Ctrl+Shift+P opens the palette from a text field, an open popup, or a modal dialog alike; F1 opens
help unless a scope on the way routes it or a control consumes it. `toolbar.bold` answers to Ctrl+B
and F6 and to nothing else, and `assignee.next` answers to J beside its own ArrowDown.

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
