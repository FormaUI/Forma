namespace Forma.Blazor.Components.Composer;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Css;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;

/// <summary>The bar of the commands of the plugins a host registered, with an overflow menu.</summary>
public sealed partial class FormaComposerPluginsBar : FormaElementBase
{
    /// <summary>The number of commands shown before the rest move into the overflow menu.</summary>
    public const int InlineLimit = 8;

    /// <summary>The items the bar carries.</summary>
    [Parameter] public IReadOnlyList<ToolbarItem> Items { get; set; } = [];

    /// <summary>Whether every control on the bar is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The key bindings each command's shortcut is read from.</summary>
    [Parameter] public ComposerKeyBindingCatalog KeyBindings { get; set; } = ComposerKeyBindingCatalog.Empty;

    /// <summary>Callback invoked with the command an item raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>Whether the bar is hidden.</summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>Whether hiding and showing the bar is animated.</summary>
    [Parameter] public bool Animated { get; set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    private IReadOnlyList<ToolbarItem> _visibleItems = [];

    private IReadOnlyList<ToolbarAction> _overflowingActions = [];

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _visibleItems = SplitVisible();

        // Skips before filtering, since the visible split counts every leaf toward the limit.
        _overflowingActions = [.. Items.SelectMany(GetLeaves).Skip(InlineLimit).OfType<ToolbarAction>()];
    }

    private List<ToolbarItem> SplitVisible()
    {
        var remainingCount = InlineLimit;
        List<ToolbarItem> visibleItems = [];

        foreach (var item in Items)
        {
            if (remainingCount <= 0)
            {
                break;
            }

            if (item is ToolbarGroup group)
            {
                visibleItems.Add(group.Items.Count <= remainingCount ? group : group with { Items = [.. group.Items.Take(remainingCount)] });
                remainingCount -= group.Items.Count;
            }
            else
            {
                visibleItems.Add(item);
                remainingCount -= 1;
            }
        }

        return visibleItems;
    }

    private static IEnumerable<ToolbarItem> GetLeaves(ToolbarItem item)
        => item is ToolbarGroup group ? group.Items : [item];

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add("forma-composer-plugins-row");
    }
}
