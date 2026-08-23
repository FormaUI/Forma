namespace Forma.UI.Blazor.Components.Bars;

using Forma.UI.Blazor.Components.Buttons;

using Microsoft.AspNetCore.Components;

/// <summary>A toolbar of grouped, item-driven commands, built on <see cref="FormaCommandArea"/> and <see cref="FormaCommandBar"/>.</summary>
public sealed partial class FormaToolbar : ComponentBase
{
    private static readonly FormaButtonOptions MenuTriggerOptions =
        new() { Variant = ButtonVariant.Ghost, Size = ButtonSize.Mini };

    /// <summary>The accessible name of the toolbar.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>The items the toolbar renders; a <see cref="FormaToolbarGroup"/> clusters the items it holds.</summary>
    [Parameter] public IReadOnlyList<FormaToolbarItem> Items { get; set; } = [];

    /// <summary>Whether every control in the toolbar is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether an item with an icon also shows its label; icon-only when false.</summary>
    [Parameter] public bool ShowLabels { get; set; }

    /// <summary>Callback invoked with the command an item raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>Content pinned past the end of the track, staying put while the commands scroll.</summary>
    [Parameter] public RenderFragment? TrailingContent { get; set; }

    /// <summary>Accessible name for the button scrolling toward the start.</summary>
    [Parameter] public string? ScrollBackLabel { get; set; }

    /// <summary>Accessible name for the button scrolling toward the end.</summary>
    [Parameter] public string? ScrollForwardLabel { get; set; }

    /// <summary>Extra classes applied to the underlying <see cref="FormaCommandBar"/>.</summary>
    [Parameter] public string? Class { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);

        foreach (var item in Items.SelectMany(Leaves))
        {
            if (item is FormaToolbarGroup nested)
            {
                throw new ArgumentException($"Toolbar group '{nested.Id}' is nested inside another group; groups do not nest.", nameof(Items));
            }

            if (item is FormaToolbarMenu { Options.Count: 0 } menu)
            {
                throw new ArgumentException($"Toolbar menu '{menu.Id}' must offer at least one option.", nameof(Items));
            }
        }
    }

    /// <summary>Every item a top-level entry contributes, so a group is checked through its own contents.</summary>
    private static IEnumerable<FormaToolbarItem> Leaves(FormaToolbarItem item)
        => item is FormaToolbarGroup group ? group.Items : [item];

    private IEnumerable<Row> Rows
        => Items.Select(item => item is FormaToolbarGroup group
            ? new Row(group.Id, group, group.Items)
            : new Row(item.Id, null, [item]));

    private static FormaToolbarOption GetActiveOption(FormaToolbarMenu menu)
        => menu.Options.FirstOrDefault(option => option.IsActive) ?? menu.Options[0];

    private static string GetTooltip(FormaToolbarItem item)
    {
        var label = item.Tooltip ?? item.Title;

        return item is FormaToolbarAction { ShortcutDisplay: { } shortcut }
            ? $"{label} ({shortcut})"
            : label;
    }

    /// <summary>The hover text for an action, or null when its visible label already says the same thing.</summary>
    private static string? GetActionTitle(FormaToolbarAction action, bool showsLabel)
    {
        var tooltip = GetTooltip(action);

        return showsLabel && tooltip == action.Title ? null : tooltip;
    }

    private static Dictionary<string, object>? ToParameters(IReadOnlyDictionary<string, object>? parameters)
        => parameters is null ? null : new Dictionary<string, object>(parameters);

    /// <summary>One rendered run of items: a group and its contents, or a single ungrouped item.</summary>
    private sealed record Row(string Key, FormaToolbarGroup? Group, IReadOnlyList<FormaToolbarItem> Items);
}
