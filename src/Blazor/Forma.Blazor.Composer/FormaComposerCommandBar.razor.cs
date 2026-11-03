namespace Forma.Blazor.Components.Composer;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;

/// <summary>An editor's command bar, which lays its items on a <see cref="Bars.FormaToolbar"/>.</summary>
public sealed partial class FormaComposerCommandBar : FormaComponentBase
{
    /// <summary>The accessible name of the bar.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>The items laid out on the bar, in the reader's language.</summary>
    [Parameter] public IReadOnlyList<ToolbarItem> Items { get; set; } = [];

    /// <summary>Whether the bar opens with the undo and redo commands.</summary>
    [Parameter] public bool ShowHistory { get; set; }

    /// <summary>Whether every control on the bar is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The key bindings each action's shortcut is read from.</summary>
    [Parameter] public ComposerKeyBindingCatalog KeyBindings { get; set; } = ComposerKeyBindingCatalog.Empty;

    /// <summary>Callback invoked with the command an item raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>The content rendered after the items, such as an overflow toggle.</summary>
    [Parameter] public RenderFragment? TrailingContent { get; set; }

    /// <summary>The toggles and options the bar's plugin reports as active.</summary>
    [Parameter] public IReadOnlyCollection<string> PluginStates { get; set; } = [];

    /// <summary>The context of the composer the bar belongs to.</summary>
    [CascadingParameter] public ComposerContext? Composer { get; set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    private bool CanUndo
        => Composer?.History.CanUndo ?? false;

    private bool CanRedo
        => Composer?.History.CanRedo ?? false;

    /// <summary>The items the bar renders: the history group where shown, then the items with their shortcuts.</summary>
    private IReadOnlyList<ToolbarItem> RenderedItems
    {
        get
        {
            List<ToolbarItem> items = [];

            if (ShowHistory)
            {
                items.Add(new ToolbarGroup
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

    private ToolbarCommand BuildHistoryCommand(string id, string title, Glyph icon, string command, bool isDisabled)
    {
        var shortcut = KeyBindings.FindByCommand(command);

        return new ToolbarCommand
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

    // Adds each action's current shortcut.
    private ToolbarItem Patch(ToolbarItem item) => item switch
    {
        ToolbarGroup group => group with { Items = [.. group.Items.Select(Patch)] },
        ToolbarAction action => action with
        {
            ShortcutDisplay = KeyBindings.FindByCommand(action.Command)?.Display,
            ShortcutAriaKeys = KeyBindings.FindByCommand(action.Command)?.AriaKeyShortcuts,
        },
        _ => item,
    };

    // The composer's own toggles.
    private const string MaximizeToggleId = "maximize";
    private const string SelectionBarToggleId = "selection-bar";

    /// <summary>The states the toolbar shows as active: the selection's styles, the composer's chrome and the plugin states.</summary>
    private IReadOnlySet<string> ActiveStates
    {
        get
        {
            var states = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (Composer?.Selection.ActiveStyles is { } styles)
            {
                states.UnionWith(styles);
            }

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
