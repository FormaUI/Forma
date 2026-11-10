# Forma.Framework.Input

The types that describe input routing in Forma: the routes an element answers, the bindings that
raise them, how an open surface treats Escape and a press away, the policy a host sets for the whole
application, and the records an action arrives with. The input router resolves every key, press,
wheel turn and drop on the page to one owner, so a component implements its actions and the router
decides which component receives each one.

The package comes with `formaui-net.Blazor`. Its types are in `Forma.Framework.Input`, with each
family's binding under `.Keyboard`, `.Pointer`, `.Click`, `.Wheel` and `.Drop`.

## Setting the application's policy

The host's Forma registration takes an `InputRouterOptions` as its `InputRouting`. A host uses it to add
shortcuts of its own and to remap the library's, and registers an `IInputActionHandler` to perform its actions:

```csharp
using Forma.Blazor;
using Forma.Framework.Input;
using Forma.Framework.Input.Keyboard;

builder.AddFormaUIServer(options => options.InputRouting = new InputRouterOptions
{
    PreemptiveRoutes = [new InputRoute("app.palette", KeyBinding.Shortcut("p", shift: true))],
    Routes = [new InputRoute("app.help", new KeyBinding("F1"))],
    Bindings = new Dictionary<string, InputBindingOverride>
    {
        ["search-box.search"] = new() { Add = [new KeyBinding("F3")] },
    },
});
builder.Services.AddScoped<IInputActionHandler, AppShortcuts>();
```

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

Ctrl+Shift+P (Cmd+Shift+P on Apple platforms) opens the palette from anywhere on the page, a text
field or a modal dialog included. F1 opens help when no component on the way answers it, and a
search box searches on F3 as well as Enter.

| `InputRouterOptions` | What it is |
| --- | --- |
| `Escape` | What an open surface does with Escape unless its profile says otherwise; `Dismiss` by default. |
| `CloseRequest` | What an open surface does with a platform close request; `null` does what Escape does. |
| `PressAway` | What an open surface does when pressed away from; `Dismiss` by default. |
| `Routes` | The host's routes, answered once every component has declined the input. |
| `PreemptiveRoutes` | The host's routes, answered before any component. |
| `Bindings` | The bindings the host changes, keyed by action. |

An `InputBindingOverride` either replaces an action's bindings, through `Replace` or
`InputBindingOverride.ReplaceWith(bindings)`, or adjusts them through `Add` and `Remove`;
`InputBindingOverride.Unbound` leaves the action with none.

## Declaring routes on a component

A component describes what its element answers as an `InputProfile` and registers it on the element
it owns. `FormaInputScope`, in `Forma.Blazor.Components.Interaction`, does that from markup; a
library component that owns its root element uses `InputRegistration` from
`Forma.Framework.Input.Interop`.

A list that answers the arrow keys and Enter while open, and closes on Escape or on a press
elsewhere, which still reaches whatever was pressed:

```razor
@using Forma.Blazor.Components.Interaction
@using Forma.Framework.Input
@using Forma.Framework.Input.Keyboard

<FormaInputScope Profile="ListProfile" Active="_open" OnAction="HandleAction" OnDismiss="_ => _open = false">
    <ul role="listbox">
        @for (var i = 0; i < _members.Length; i++)
        {
            <li role="option" aria-selected="@(i == _index ? "true" : "false")">@_members[i]</li>
        }
    </ul>
</FormaInputScope>

@code {
    private static readonly InputProfile ListProfile = new()
    {
        Escape = DismissBehavior.Dismiss,
        PressAway = PressAwayBehavior.PassThrough,
        InputRoutes =
        [
            new InputRoute("assignee.next", new KeyBinding("ArrowDown")),
            new InputRoute("assignee.previous", new KeyBinding("ArrowUp")),
            new InputRoute("assignee.choose", new KeyBinding("Enter")),
        ],
    };

    private readonly string[] _members = ["Ada", "Grace", "Linus"];
    private bool _open = true;
    private int _index;

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
            case "assignee.choose":
                _open = false;
                break;
        }
    }
}
```

| `InputProfile` | What it is |
| --- | --- |
| `InputRoutes` | The routes the element answers. |
| `Modal` | Whether the element is a barrier that nothing opened beneath it answers through. |
| `Escape`, `CloseRequest` | A `DismissBehavior`, `Dismiss`, `Cancel` or `Ignore`, for Escape and a platform close request while the element is foremost; `null` takes the host's policy. |
| `PressAway` | A `PressAwayBehavior` while the element is foremost: `Dismiss` closes it and swallows the press, `PassThrough` closes it and lets the press reach what was pressed, `Ignore` keeps it open; `null` takes the host's policy. |
| `Boundary` | The event types that stop at the element when none of its routes matches. |
| `ItemAttribute` | The attribute naming the item an input lands on, whose value the input carries as `Item`. |

A dismissal reaches the owner as the behavior declared, with the `DismissReason` that raised it:
`Escape`, `PressAway` or `CloseRequest`.

## Routes and bindings

An `InputRoute(action, bindings)` names an action as `{owner}.{action}` in lowercase letters, digits
and hyphens, such as `composer.bold`, and lists the bindings that raise it by default.
`InputRoute.Qualify(owner, name)` builds that name from free text, so `Insert Table` under `toolbar`
is `toolbar.insert-table`. `Priority` decides between routes that match equally well, and
`PreventDefault` and `Propagation` override what the router does with the browser's default and with
scopes further out.

| Binding | What it matches |
| --- | --- |
| `KeyBinding(key)` | A key on `keydown`, or on `keyup` with `Released`. `KeyBinding.Shortcut(key, shift)` adds the platform's primary modifier. |
| `TypingBinding` | Any printable character typed other than a space. |
| `ClickBinding` | A click, or a double click with `IsDouble`, by `Button`. |
| `PointerBinding` | A press, or a release or move by `Phase`, by `Button`, `Buttons` held and `PointerType`. `PointerBinding.Down` and `PointerBinding.Drag` cover the common cases. |
| `WheelBinding` | A wheel turn along an `Axis`; `WheelBinding.Any` matches every turn. |
| `DropBinding` | A drop, or a drag over with `Over`, carrying any of the `Data` kinds; `DropBinding.Files` matches dropped files. |

`Modifiers` on a binding is a `ModifierRequirement`: `Control`, `Alt`, `Shift` and `Meta` each
required, forbidden or left to the family's default, and `Primary` for Command on Apple platforms and
Control elsewhere. `ModifierRequirement.Shortcut` requires `Primary`.

## How the router chooses

- A key, press, wheel turn or drop goes to the nearest registered element, from the event's target
  outward, with a route that matches it. An open surface keeps every such input that reaches it.
- Escape, a press away and a platform close request go to the foremost open surface alone.
- A native control keeps the input it handles itself, such as the caret keys in a text field.
- Among matching bindings, the one stating the most wins, so Shift+ArrowDown takes precedence over
  ArrowDown; then the higher `Priority`, then the earlier registration.
- The host's `PreemptiveRoutes` are asked before all of this, and its `Routes` after every component
  has declined.

## What an action carries

The input arrives as the `InputInfo` of its family, each carrying the DOM event's `Type`:
`InputKeyInfo` with `Key`, `Code` and `Modifiers`; `InputPointerInfo` with `PointerId`,
`PointerType`, `Button`, `Buttons`, `ClientX`, `ClientY` and `Modifiers`; `InputWheelInfo` with
`DeltaX`, `DeltaY`, `DeltaMode` and `Modifiers`; and `InputDropInfo` with the `Data` kinds, `ClientX`
and `ClientY`. `ModifierKeys` says which of `Control`, `Alt`, `Shift`, `Meta` and `Primary` were held.
Where the profile names an `ItemAttribute`, every family carries `Item`, that attribute's value on the
nearest element under the input that holds it, so a press on a list's row names the row pressed.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
