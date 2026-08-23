namespace Forma.UI.Blazor.Components.Layout;

using Forma.UI.Blazor.Components.Buttons;

using Microsoft.AspNetCore.Components;

/// <summary>A toolbar of grouped, item-driven commands, built on <see cref="FormaCommandArea"/> and <see cref="FormaCommandBar"/>.</summary>
public sealed partial class FormaToolbar : ComponentBase
{
    private static readonly FormaButtonOptions MenuTriggerOptions =
        new() { Variant = ButtonVariant.Ghost, Size = ButtonSize.Mini };

    /// <summary>The accessible name of the toolbar.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>The commands the toolbar renders, clustered by their own <see cref="FormaToolbarItem.Group"/>.</summary>
    [Parameter] public IReadOnlyList<FormaToolbarItem> Items { get; set; } = [];

    /// <summary>Whether every control in the toolbar is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

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

    private IReadOnlyList<IReadOnlyList<FormaToolbarItem>> Groups
        => Items.Count == 0
            ? []
            : [.. Items.GroupBy(item => item.Group ?? string.Empty).Select(group => (IReadOnlyList<FormaToolbarItem>)[.. group])];

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentException.ThrowIfNullOrWhiteSpace(Label);

    private async Task HandleItemClickAsync(FormaToolbarItem item)
    {
        if (item.Command is { } command)
        {
            await OnCommand.InvokeAsync(command);
        }
    }

    private static string GetActiveOptionTitle(FormaToolbarItem item)
        => (item.Options.FirstOrDefault(option => option.IsActive) ?? item.Options[0]).Title;

    private static string? GetActiveSwatch(FormaToolbarItem item)
        => (item.Options.FirstOrDefault(option => option.IsActive) ?? item.Options[0]).Swatch;

    private static string GetTooltip(FormaToolbarItem item)
        => item.ShortcutDisplay is { } shortcut
            ? $"{item.Title} ({shortcut})"
            : item.Title;
}
