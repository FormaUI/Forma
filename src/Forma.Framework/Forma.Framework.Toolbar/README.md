# Forma.Framework.Toolbar

The item model Forma's toolbars render: a toolbar described as a list of records, each a command,
a toggle, a menu of options, a readout or a component of the caller's own, grouped or standing
alone. Every item that acts names a command, so the caller handles one callback and dispatches on
the command string.

## Use it

The package comes with `formaui-net.Blazor`, and its types live in the
`Forma.Framework.Toolbar` namespace. A list of `ToolbarItem`s is what `FormaToolbar`,
`FormaSearchToolbar`, `FormaSelectionToolbar` and `FormaStatusStrip` take as `Items`, and what
`FormaWindow` and `FormaChrome` take as `Commands` for their title bars. Each raises the chosen
item's command through `OnCommand`.

A formatting toolbar over an editable area: inline styles with shortcuts, a command that needs a
link to act on, a heading menu, and an announced word count. One set of identifiers reports which
toggle is on, which heading is chosen, and whether the link command is available.

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

    private IReadOnlyList<ToolbarItem> Items =>
    [
        new ToolbarGroup
        {
            Id = "inline",
            Title = "Inline Style",
            Items =
            [
                new ToolbarToggle { Id = "bold", Title = "Bold", Icon = Glyph.Bold, Command = "format.bold", ShortcutDisplay = "Ctrl+B", ShortcutAriaKeys = "Control+B" },
                new ToolbarToggle { Id = "link", Title = "Link", Icon = Glyph.Link, Command = "format.link", ShortcutDisplay = "Ctrl+K", ShortcutAriaKeys = "Control+K" },
                new ToolbarCommand { Id = "unlink", Title = "Remove Link", Icon = Glyph.RemoveLink, Command = "format.unlink", RequiresState = "link" },
            ],
        },
        new ToolbarMenu
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
        new ToolbarReadout { Id = "words", Title = "Word count", Value = $"{WordCount} words", IsAnnounced = true },
    ];

    // The element reference is captured after the first render, so the toolbar renders once more to receive it.
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

The same records on a status strip:

```razor
<FormaStatusStrip Label="Document Status" Items="Status" OnCommand="HandleCommand" />

@code {
    private bool _isSaved;
    private int _wordCount;

    private IReadOnlyList<ToolbarItem> Status =>
    [
        new ToolbarReadout { Id = "saved", Title = "Save state", Value = _isSaved ? "Saved" : "Unsaved", IsAnnounced = true },
        new ToolbarReadout { Id = "words", Title = "Word count", Value = $"{_wordCount} words" },
        new ToolbarCommand { Id = "save", Title = "Save", Icon = Glyph.Save, Command = "document.save", IsDisabled = _isSaved },
    ];

    private void HandleCommand(string command)
        => _isSaved = command == "document.save";
}
```

## The items

| Type | What it is |
| --- | --- |
| `ToolbarCommand` | An action that fires once when activated. |
| `ToolbarToggle` | An action with an on/off state, and an alternate title and icon while on. |
| `ToolbarMenu` | A menu of `ToolbarOption`s, each raising its own command. |
| `ToolbarReadout` | A value on display, optionally announced to a screen reader as it changes. |
| `ToolbarCustomItem` | A component the caller names, rendered in the item's place. |
| `ToolbarGroup` | A named cluster of items, drawn as one group and set apart from its neighbors by a rule. |

A list mixes bare items and groups; a group holds items only, and a menu offers at least one
option.

Every item, through `ToolbarItem`:

| Member | What it is |
| --- | --- |
| `Id` | The item's unique identifier within its group. |
| `Title` | The item's label and accessible name, localized by the caller. |
| `Tooltip` | The hover text, or `null` to show `Title`. |
| `Icon` | The icon shown for the item, or `null` to show `Title` alone. |
| `IsDisabled` | Whether this item is disabled. |

`ToolbarCommand` and `ToolbarToggle`, through `ToolbarAction`:

| Member | What it is |
| --- | --- |
| `Command` | The command raised when the action is activated. |
| `ShortcutDisplay` | The shortcut shown in the tooltip, such as `Ctrl+B`. |
| `ShortcutAriaKeys` | The shortcut in `aria-keyshortcuts` notation, such as `Control+B`, which binds it. |
| `RequiresState` | The state the host must report before the action can run. |

`ToolbarToggle` adds `IsActive`, `AlternateTitle` and `AlternateIcon`. `ToolbarMenu` has
`Options`, each a `ToolbarOption` with its own `Id`, `Title`, `Icon` and `Command`, plus
`IsActive` for the option matching the current state, `PreviewStyle`, inline CSS previewing the
option on its own label, and `Swatch`, a CSS color drawn beside it. `ToolbarReadout` has `Value`
and `IsAnnounced`. `ToolbarCustomItem` has `Component`, the component type, and `Parameters`,
handed to it as given. `ToolbarGroup` has `Items`.

## What the toolbar does with them

- **Commands.** Clicking a control, picking an option or pressing a bound shortcut raises its
  `Command` through `OnCommand`. The toolbar holds a command back while it is `Disabled`, while
  the item is `IsDisabled`, or while the state it requires is unreported.
- **State.** `ActiveStates` is the set of identifiers the host reports. A toggle is on, and an
  option active, when its `IsActive` is set or its `Id` is reported, and an action runs once its
  `RequiresState` is reported. A caller sets `IsActive` on the records it builds, or keeps one set
  of identifiers and hands it over.
- **Shortcuts.** An action's `ShortcutAriaKeys` is bound once `Target` names an element, and
  answers while focus is within it; `Control` and `Meta` both mean the platform's shortcut key.
  `ShortcutDisplay` is appended to the tooltip, as `Bold (Ctrl+B)`.
- **Labels.** An item with an icon is drawn icon-only, with its title as its accessible name,
  until `ShowLabels` puts the label beside it. A menu's trigger shows the active option, or the
  first while none is active.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
