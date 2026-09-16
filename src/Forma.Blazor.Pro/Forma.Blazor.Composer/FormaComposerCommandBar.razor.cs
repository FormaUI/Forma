namespace Forma.Blazor.Components.Composer;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;

/// <summary>One command bar belonging to an editor, laying its items — and, optionally, undo/redo — on a <see cref="FormaToolbar"/>.</summary>
public sealed partial class FormaComposerCommandBar : FormaComponentBase
{
    /// <summary>The accessible name of the bar.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>The items laid out on the bar, already in the reader's language, ready for a FormaToolbar to render as given.</summary>
    [Parameter] public IReadOnlyList<FormaToolbarItem> Items { get; set; } = [];

    /// <summary>Whether the bar opens with the undo and redo commands.</summary>
    [Parameter] public bool ShowHistory { get; set; }

    /// <summary>Whether every control on the bar is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The key bindings each action's shortcut is read from.</summary>
    [Parameter] public ComposerKeyBindingCatalog KeyBindings { get; set; } = ComposerKeyBindingCatalog.Empty;

    /// <summary>Callback invoked with the command an item raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>Content rendered after the command groups, such as an overflow toggle.</summary>
    [Parameter] public RenderFragment? TrailingContent { get; set; }

    /// <summary>The toggles and options the bar's own plugin reports as active, beside what the selection reports.</summary>
    [Parameter] public IReadOnlyCollection<string> PluginStates { get; set; } = [];

    /// <summary>The composer state cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? Composer { get; set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    private bool CanUndo
        => Composer?.History.CanUndo ?? false;

    private bool CanRedo
        => Composer?.History.CanRedo ?? false;

    /// <summary>What the bar actually renders: the history group first when shown, then Items patched with the shortcuts they currently answer to.</summary>
    private IReadOnlyList<FormaToolbarItem> RenderedItems
    {
        get
        {
            List<FormaToolbarItem> items = [];

            if (ShowHistory)
            {
                items.Add(new FormaToolbarGroup
                {
                    Id = "history",
                    Title = L["History"],
                    Items =
                    [
                        BuildHistoryCommand("undo", "Undo", Glyph.Undo, ComposerCommands.Undo, isDisabled: !CanUndo),
                        BuildHistoryCommand("redo", "Redo", Glyph.Redo, ComposerCommands.Redo, isDisabled: !CanRedo),
                    ],
                });
            }

            items.AddRange(Items.Select(Patch));

            return items;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentException.ThrowIfNullOrWhiteSpace(Label);

    private FormaToolbarCommand BuildHistoryCommand(string id, string title, Glyph icon, string command, bool isDisabled)
    {
        var shortcut = KeyBindings.FindByCommand(command);

        return new FormaToolbarCommand
        {
            Id = id,
            Title = L[title],
            Icon = icon,
            Command = command,
            IsDisabled = isDisabled,
            ShortcutDisplay = shortcut?.Display,
            ShortcutAriaKeys = shortcut?.AriaKeyShortcuts,
        };
    }

    // Layers on the state only render time knows: which shortcut a command currently answers to.
    private FormaToolbarItem Patch(FormaToolbarItem item) => item switch
    {
        FormaToolbarGroup group => group with { Items = [.. group.Items.Select(Patch)] },
        FormaToolbarAction action => action with
        {
            ShortcutDisplay = KeyBindings.FindByCommand(action.Command)?.Display,
            ShortcutAriaKeys = KeyBindings.FindByCommand(action.Command)?.AriaKeyShortcuts,
        },
        _ => item,
    };

    // The engine's own toggles, whose state it reports rather than the selection.
    private const string MaximizeToggleId = "maximize";
    private const string SelectionBarToggleId = "selection-bar";

    /// <summary>What the toolbar treats as active: the styles the selection reports, plus the engine's own chrome state.</summary>
    private IReadOnlySet<string> ActiveStates
    {
        get
        {
            var states = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (Composer?.Selection.ActiveStyles is { } styles)
            {
                states.UnionWith(styles);
            }

            // Engine-owned chrome rather than a text style, so the selection never reports it.
            if (Composer?.IsMaximized ?? false)
            {
                states.Add(MaximizeToggleId);
            }

            if (Composer?.IsSelectionBarShown ?? true)
            {
                states.Add(SelectionBarToggleId);
            }

            states.UnionWith(PluginStates);

            return states;
        }
    }
}
