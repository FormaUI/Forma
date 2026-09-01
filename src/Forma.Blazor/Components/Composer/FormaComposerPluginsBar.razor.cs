namespace Forma.Blazor.Components.Composer;

using Forma.Features.Composer.Editing;
using Forma.Framework.Localization;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;

/// <summary>The bar carrying the commands of the plugins a host registered, overflowing the surplus into a menu.</summary>
public sealed partial class FormaComposerPluginsBar : ComponentBase
{
    /// <summary>The number of commands shown before the rest move into the overflow menu.</summary>
    public const int InlineLimit = 8;

    /// <summary>The id the bar's element carries, so a control elsewhere can point at it.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>The items the bar carries.</summary>
    [Parameter] public IReadOnlyList<FormaToolbarItem> Items { get; set; } = [];

    /// <summary>Whether every control on the bar is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The key bindings each command's shortcut is read from.</summary>
    [Parameter] public ComposerKeyBindingCatalog KeyBindings { get; set; } = ComposerKeyBindingCatalog.Empty;

    /// <summary>Callback invoked with the command an item raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>Hides the bar while keeping it mounted, independently of whether it carries any items.</summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>Animates the transition between hidden and shown instead of snapping instantly.</summary>
    [Parameter] public bool Animated { get; set; }

    [Inject] private IStringLocale<FormaComposer> L { get; set; } = default!;

    private IReadOnlyList<FormaToolbarItem> _visibleItems = [];

    private IReadOnlyList<FormaToolbarAction> _overflowingActions = [];

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _visibleItems = SplitVisible();

        // Skips before filtering, since the visible split counts every leaf toward the limit — filtering
        // first would misalign the index and drop an action that is neither shown nor in the menu.
        _overflowingActions = [.. Items.SelectMany(Leaves).Skip(InlineLimit).OfType<FormaToolbarAction>()];
    }

    private List<FormaToolbarItem> SplitVisible()
    {
        var remaining = InlineLimit;
        List<FormaToolbarItem> visible = [];

        foreach (var item in Items)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (item is FormaToolbarGroup group)
            {
                visible.Add(group.Items.Count <= remaining ? group : group with { Items = [.. group.Items.Take(remaining)] });
                remaining -= group.Items.Count;
            }
            else
            {
                visible.Add(item);
                remaining -= 1;
            }
        }

        return visible;
    }

    private static IEnumerable<FormaToolbarItem> Leaves(FormaToolbarItem item)
        => item is FormaToolbarGroup group ? group.Items : [item];

    private string GetItemTitle(FormaToolbarItem item)
        => L[item.Title].Value;
}
