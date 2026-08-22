namespace Forma.UI.Blazor.Components.Composer;

using Forma.Framework.Localization;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;

using Microsoft.AspNetCore.Components;

/// <summary>The bar carrying the commands of the plugins a host registered, overflowing the surplus into a menu.</summary>
public sealed partial class FormaComposerPluginsToolbar : ComponentBase
{
    /// <summary>The number of commands shown before the rest move into the overflow menu.</summary>
    public const int InlineLimit = 8;

    /// <summary>The id the bar's element carries, so a control elsewhere can point at it.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>The plugin command groups the bar carries.</summary>
    [Parameter] public IReadOnlyList<IReadOnlyList<ComposerToolbarItem>> Groups { get; set; } = [];

    /// <summary>Whether every control on the bar is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The key bindings each command's shortcut is read from.</summary>
    [Parameter] public ComposerKeyMap KeyMap { get; set; } = ComposerKeyMap.Empty;

    /// <summary>Callback invoked with the command an item raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    [Inject] private IStringLocale<FormaComposer> L { get; set; } = default!;

    private IReadOnlyList<IReadOnlyList<ComposerToolbarItem>> Visible
    {
        get
        {
            var remaining = InlineLimit;
            var visible = new List<IReadOnlyList<ComposerToolbarItem>>();

            foreach (var group in Groups)
            {
                if (remaining <= 0)
                {
                    break;
                }

                visible.Add(group.Count <= remaining ? group : [.. group.Take(remaining)]);
                remaining -= group.Count;
            }

            return visible;
        }
    }

    private IReadOnlyList<ComposerToolbarItem> Overflowing
        => [.. Groups.SelectMany(group => group).Skip(InlineLimit)];

    private string GetItemTitle(ComposerToolbarItem item)
        => L[item.Title].Value;
}
