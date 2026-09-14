# Forma.Framework.Toolbar

The item model `FormaToolbar` renders: a row of controls described as a list of records, each
a command, a toggle, a menu of options, a readout or a component of the caller's own, grouped
or standing alone. A caller adds a control by adding an item, and every item names the command
the toolbar raises when the control is used, so the caller handles one callback and dispatches
on the command.

The same list feeds the toolbars composed over `FormaToolbar` — `FormaSearchToolbar`,
`FormaSelectionToolbar` and `FormaStatusStrip` — and the `Commands` a host places on the title
bar of `FormaWindow` and `FormaChrome`.

The assembly ships inside `2Site.Forma.Blazor`; nothing is installed or registered for it. Its
types live in the `Forma.Framework.Toolbar` namespace.

## The items

| Type | What it is |
| --- | --- |
| `FormaToolbarItem` | The base of every item: its identity, its title, its icon, and whether it alone is disabled. |
| `FormaToolbarGroup` | A named cluster of items, drawn as one `role="group"` and parted from its neighbors by a rule. |
| `FormaToolbarAction` | The base of an item that raises a command: a command, an optional shortcut, and an optional state it needs. |
| `FormaToolbarCommand` | An action that fires once when activated. |
| `FormaToolbarToggle` | An action that reflects an on/off state, with an alternate title and icon while on. |
| `FormaToolbarMenu` | An item presenting a menu of `FormaToolbarOption`s, each raising its own command. |
| `FormaToolbarOption` | One choice inside a menu; not an item itself, so it stands only inside a `FormaToolbarMenu`. |
| `FormaToolbarReadout` | A non-interactive value, optionally announced to a screen reader as it changes. |
| `FormaToolbarCustomItem` | An item rendered by a component the caller names, in place of a standard control. |

A toolbar's `Items` list mixes bare items and groups; a group holds items and never another
group, and a menu offers at least one option. The toolbar rejects a list that breaks either.

### `FormaToolbarItem`

| Member | What it is |
| --- | --- |
| `Id` | The item's unique identifier within its group, and the key the toolbar renders it under. |
| `Title` | The item's label and accessible name, rendered as given, so the caller localizes it. |
| `Tooltip` | The hover text, or `null` to show `Title` on hover as well. |
| `Icon` | The icon shown for the item, or `null` when its title stands alone. |
| `IsDisabled` | Whether this item alone is disabled, whatever the toolbar's own `Disabled` says. |

### `FormaToolbarGroup`

| Member | What it is |
| --- | --- |
| `Items` | The items the group contains. |

### `FormaToolbarAction`

| Member | What it is |
| --- | --- |
| `Command` | The command raised when the action is activated. |
| `ShortcutDisplay` | The shortcut shown in the tooltip, such as `Ctrl+B`, or `null`. |
| `ShortcutAriaKeys` | The shortcut in `aria-keyshortcuts` notation, such as `Control+B`, or `null`. |
| `RequiresState` | The state the host must report before the action can run, or `null` when it is always available. |

`FormaToolbarCommand` adds nothing. `FormaToolbarToggle` adds:

| Member | What it is |
| --- | --- |
| `IsActive` | Whether the toggle is on. |
| `AlternateTitle` | The title shown while on, or `null` to keep `Title`. |
| `AlternateIcon` | The icon shown while on, or `null` to keep `Icon`. |

### `FormaToolbarMenu` and `FormaToolbarOption`

A menu carries `Options`, the choices it presents. Each option is:

| Member | What it is |
| --- | --- |
| `Id` | The option's unique identifier within its menu. |
| `Title` | The option's label and accessible name, rendered as given. |
| `Icon` | The icon shown beside the option, or `null`. |
| `Command` | The command raised when the option is picked. |
| `IsActive` | Whether this is the option the menu's current state matches. |
| `PreviewStyle` | Inline CSS applied to the option's own label, previewing its effect, or `null`. |
| `Swatch` | A CSS color drawn as a swatch beside the label, or `null`. |

The menu's trigger shows the active option's title, or its swatch when it has one; while no
option is active the first stands in.

### `FormaToolbarReadout`

| Member | What it is |
| --- | --- |
| `Value` | The value displayed, formatted and localized by the caller. |
| `IsAnnounced` | Whether a screen reader announces the value as it changes. |

### `FormaToolbarCustomItem`

| Member | What it is |
| --- | --- |
| `Component` | The component type rendered in the item's place. |
| `Parameters` | The parameters passed to the component, or `null` when it takes none. |

Parameter values are handed to the component as given, with no conversion, so each is already
of the parameter's own type.

## What the toolbar does with them

**Commands.** Every action and every option carries a `Command` string. Clicking the control,
picking the option, or pressing a bound shortcut raises that string through the toolbar's
`OnCommand`, and the caller dispatches on it. A command is refused while the toolbar is
`Disabled`, while the item's `IsDisabled` is set, or while the state the action requires is
absent.

**State.** The toolbar's `ActiveStates` is the set of identifiers the host currently reports.
A toggle is drawn on when its `IsActive` is set or its `Id` is reported; an option is the
active one on the same terms; an action whose `RequiresState` names a state is held back until
that state is reported. A caller either sets `IsActive` on the records it builds each render or
keeps one set of identifiers and hands it over, whichever its state lives closer to.

**Shortcuts.** An action with `ShortcutAriaKeys` is bound once the toolbar's `Target` names an
element, and answers while that element or a descendant has focus; `Control` and `Meta` both
stand for the platform's shortcut key. `ShortcutDisplay` is appended to the action's tooltip,
as `Bold (Ctrl+B)`.

**Labels.** An item with an icon is drawn icon-only, carrying its title as its accessible name,
unless the toolbar's `ShowLabels` puts the label beside the icon; an item without an icon shows
its title regardless. A menu's trigger shows its icon and the active option; a readout shows its
value. A toggle that is on shows its `AlternateTitle` and `AlternateIcon` where they are set.

## Example

A formatting toolbar over an editable area: a group of inline styles with shortcuts, one command
that needs a link to act on, a heading menu, and an announced word count. One set of identifiers
reports which toggle is on, which heading is chosen, and whether the link command is available.

```razor
@using Forma.Blazor.Components.Bars
@using Forma.Framework.Icons
@using Forma.Framework.Toolbar

<FormaToolbar Label="Formatting"
              Items="Items"
              ActiveStates="_activeStates"
              Target="_page"
              OnCommand="HandleCommand" />

<textarea @ref="_page" @bind="_text" @bind:event="oninput"></textarea>

@code {
    private const string HeadingPrefix = "heading.";

    private readonly HashSet<string> _activeStates = new(StringComparer.OrdinalIgnoreCase) { "body" };
    private ElementReference _page;
    private string _text = string.Empty;

    private int WordCount
        => _text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    private IReadOnlyList<FormaToolbarItem> Items =>
    [
        new FormaToolbarGroup
        {
            Id = "inline",
            Title = "Inline Style",
            Items =
            [
                new FormaToolbarToggle { Id = "bold", Title = "Bold", Icon = Glyph.Bold, Command = "format.bold", ShortcutDisplay = "Ctrl+B", ShortcutAriaKeys = "Control+B" },
                new FormaToolbarToggle { Id = "link", Title = "Link", Icon = Glyph.Link, Command = "format.link", ShortcutDisplay = "Ctrl+K", ShortcutAriaKeys = "Control+K" },
                new FormaToolbarCommand { Id = "unlink", Title = "Remove Link", Icon = Glyph.RemoveLink, Command = "format.unlink", RequiresState = "link" },
            ],
        },
        new FormaToolbarMenu
        {
            Id = "heading",
            Title = "Heading",
            Options =
            [
                new() { Id = "body", Title = "Body", Command = HeadingPrefix + "body" },
                new() { Id = "h1", Title = "Heading 1", Command = HeadingPrefix + "h1", PreviewStyle = "font-size:1.5em" },
                new() { Id = "h2", Title = "Heading 2", Command = HeadingPrefix + "h2", PreviewStyle = "font-size:1.25em" },
            ],
        },
        new FormaToolbarReadout { Id = "words", Title = "Word count", Value = $"{WordCount} words", IsAnnounced = true },
    ];

    // The element reference is captured after the first render, so the toolbar is rendered once more to receive it.
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            StateHasChanged();
        }
    }

    private void HandleCommand(string command)
    {
        switch (command)
        {
            case "format.bold":
                Toggle("bold");
                break;
            case "format.link":
                Toggle("link");
                break;
            case "format.unlink":
                _activeStates.Remove("link");
                break;
            case var heading when heading.StartsWith(HeadingPrefix, StringComparison.Ordinal):
                _activeStates.RemoveWhere(state => state is "body" or "h1" or "h2");
                _activeStates.Add(heading[HeadingPrefix.Length..]);
                break;
        }
    }

    private void Toggle(string state)
    {
        if (!_activeStates.Remove(state))
        {
            _activeStates.Add(state);
        }
    }
}
```

A status strip of readouts and one command, the same records on a different bar:

```razor
@using Forma.Blazor.Components.Bars
@using Forma.Framework.Icons
@using Forma.Framework.Toolbar

<FormaStatusStrip Label="Document Status" Items="Status" OnCommand="HandleCommand" />

@code {
    private bool _isSaved;
    private int _wordCount;

    private IReadOnlyList<FormaToolbarItem> Status =>
    [
        new FormaToolbarReadout { Id = "saved", Title = "Save state", Value = _isSaved ? "Saved" : "Unsaved", IsAnnounced = true },
        new FormaToolbarReadout { Id = "words", Title = "Word count", Value = $"{_wordCount} words" },
        new FormaToolbarCommand { Id = "save", Title = "Save", Icon = Glyph.Save, Command = "document.save", IsDisabled = _isSaved },
    ];

    private void HandleCommand(string command)
        => _isSaved = command == "document.save";
}
```

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
