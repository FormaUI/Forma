namespace Forma.UI.Blazor.Components.Composer;

using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;

using Microsoft.AspNetCore.Components;

/// <summary>The command bar belonging to one editing surface, giving the surface's own commands and its plugins' commands a row each.</summary>
public sealed partial class FormaComposerToolbar : ComponentBase
{
    /// <summary>The number of commands a bar carries before the next row starts one of its own.</summary>
    public const int CommandsPerBar = 12;

    /// <summary>The editing surface this toolbar belongs to.</summary>
    [Parameter, EditorRequired] public IComposerEditorPlugin Editor { get; set; } = default!;

    /// <summary>The feature plugins on offer; those scoped to other surfaces are left out.</summary>
    [Parameter] public IReadOnlyList<IComposerPlugin> Plugins { get; set; } = [];

    /// <summary>Whether every control on the toolbar is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The key bindings each action's shortcut is read from.</summary>
    [Parameter] public ComposerKeyMap KeyMap { get; set; } = ComposerKeyMap.Empty;

    /// <summary>Callback invoked with the command an action raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    private ComposerToolbarLayout _layout = default!;
    private readonly string _pluginsId = $"composer-plugins-{Guid.NewGuid():N}";
    private bool _arePluginsShown;

    /// <summary>The bars to render, small rows folded together so none is spent on a handful of commands.</summary>
    private IReadOnlyList<ComposerToolbarRowLayout> Bars
    {
        get
        {
            var bars = new List<ComposerToolbarRowLayout>();

            foreach (var row in EditorRows.Where(row => row.Groups.Count > 0))
            {
                var count = row.Groups.Sum(group => group.Count);
                var last = bars.Count > 0 ? bars[^1] : null;

                if (last is not null && last.Groups.Sum(group => group.Count) + count <= CommandsPerBar)
                {
                    bars[^1] = last with { Groups = [.. last.Groups, .. row.Groups] };
                }
                else
                {
                    bars.Add(row);
                }
            }

            return bars.Count > 0 ? bars : [_layout.Rows.First(row => row.IsBuiltIn)];
        }
    }

    private IEnumerable<ComposerToolbarRowLayout> EditorRows
        => _layout.Rows.Where(row => row.Id != ComposerRows.Plugins);

    private IReadOnlyList<IReadOnlyList<ComposerToolbarItem>> PluginGroups
        => _layout.Rows.FirstOrDefault(row => row.Id == ComposerRows.Plugins)?.Groups ?? [];

    private void TogglePlugins()
        => _arePluginsShown = !_arePluginsShown;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Editor);
        _layout = ComposerToolbarLayout.Create(Editor, Plugins);
    }
}
