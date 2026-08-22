namespace Forma.UI.Blazor.Components.Composer;

using Forma.Framework.Localization;
using Forma.UI.Blazor.Components.Buttons;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;

using Microsoft.AspNetCore.Components;

/// <summary>One row of composer commands, scrolling behind arrows when the commands outgrow the width.</summary>
public sealed partial class FormaComposerCommandRow : ComponentBase
{
    private static readonly FormaButtonOptions MenuTriggerOptions =
        new() { Variant = ButtonVariant.Ghost, Size = ButtonSize.Mini };
    /// <summary>The accessible name of the row.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>The command groups laid out in the row, separated by a rule.</summary>
    [Parameter] public IReadOnlyList<IReadOnlyList<ComposerToolbarItem>> Groups { get; set; } = [];

    /// <summary>Whether the row opens with the undo and redo buttons.</summary>
    [Parameter] public bool ShowHistory { get; set; }

    /// <summary>Whether every control in the row is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The key bindings each action's shortcut is read from.</summary>
    [Parameter] public ComposerKeyMap KeyMap { get; set; } = ComposerKeyMap.Empty;

    /// <summary>Callback invoked with the command an action raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>Content rendered after the command groups, such as an overflow menu.</summary>
    [Parameter] public RenderFragment? TrailingContent { get; set; }

    /// <summary>The shared composer context cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? Context { get; set; }

    [Inject] private IStringLocale<FormaComposer> L { get; set; } = default!;

    private bool CanUndo
        => Context?.History.CanUndo ?? false;

    private bool CanRedo
        => Context?.History.CanRedo ?? false;

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentException.ThrowIfNullOrWhiteSpace(Label);

    private async Task HandleItemClickAsync(ComposerToolbarItem item)
    {
        if (item.Command is { } command)
        {
            await OnCommand.InvokeAsync(command);
        }
    }

    private bool IsItemActive(ComposerToolbarItem item)
        => Context?.Selection.ActiveStyles.Contains(item.Id) ?? false;

    private bool IsChoiceActive(ComposerToolbarOption choice)
        => Context?.Selection.ActiveStyles.Contains(choice.Id) ?? false;

    private string GetActiveChoiceTitle(ComposerToolbarItem item)
    {
        var active = item.Options.FirstOrDefault(IsChoiceActive) ?? item.Options[0];
        return L[active.Title].Value;
    }

    private string? GetActiveSwatch(ComposerToolbarItem item)
        => (item.Options.FirstOrDefault(IsChoiceActive) ?? item.Options[0]).Swatch;

    private string GetItemTitle(ComposerToolbarItem item)
        => L[item.Title].Value;

    private string GetTooltip(string label, string? command)
        => command is not null && KeyMap.Find(command) is { } binding
            ? $"{label} ({binding.Display})"
            : label;

    private string? GetKeyShortcuts(string? command)
        => command is not null ? KeyMap.Find(command)?.AriaKeyShortcuts : null;
}
