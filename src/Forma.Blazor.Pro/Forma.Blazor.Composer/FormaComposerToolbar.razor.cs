namespace Forma.Blazor.Components.Composer;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;

/// <summary>The command bar belonging to one editor or one view over it, giving its own commands and its plugins' commands a row each.</summary>
public sealed partial class FormaComposerToolbar : FormaComponentBase
{
    /// <summary>The number of commands a bar carries before the next row starts one of its own.</summary>
    public const int CommandsPerBar = 12;

    /// <summary>The editor this toolbar belongs to, or whose view it belongs to.</summary>
    [Parameter, EditorRequired] public IComposerEditorPlugin Editor { get; set; } = default!;

    /// <summary>The view this toolbar belongs to, or null when it belongs to the editor itself.</summary>
    [Parameter] public IComposerViewPlugin? View { get; set; }

    /// <summary>The feature plugins in play on this bar's surface.</summary>
    [Parameter] public IReadOnlyList<IComposerPlugin> Plugins { get; set; } = [];

    /// <summary>Whether every control on the toolbar is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The key bindings each action's shortcut is read from.</summary>
    [Parameter] public ComposerKeyBindingCatalog KeyBindings { get; set; } = ComposerKeyBindingCatalog.Empty;

    /// <summary>Callback invoked with the command an action raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    private ComposerToolbarLayout _layout = default!;
    private bool _arePluginsShown;

    private string PluginsId
        => $"composer-plugins-{Context.RuntimeId:N}";

    /// <summary>The plugin whose bar this is: the view when one shows, and otherwise the editor.</summary>
    private IComposerPlugin Owner
        => View ?? (IComposerPlugin)Editor;

    /// <summary>The bars to render, small rows folded together so none is spent on a handful of commands.</summary>
    private IReadOnlyList<ComposerToolbarRowLayout> Bars
    {
        get
        {
            var bars = new List<ComposerToolbarRowLayout>();

            foreach (var row in EditorRows.Where(row => row.Items.Count > 0))
            {
                var count = CountLeaves(row.Items);
                var last = bars.Count > 0 ? bars[^1] : null;

                if (last is not null && CountLeaves(last.Items) + count <= CommandsPerBar)
                {
                    bars[^1] = last with { Items = [.. last.Items, .. row.Items] };
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
        => _layout.Rows.Where(row => row.Id is not (ComposerRows.Plugins or ComposerRows.Selection));

    private IReadOnlyList<FormaToolbarItem> PluginItems
        => _layout.Rows.FirstOrDefault(row => row.Id == ComposerRows.Plugins)?.Items ?? [];

    private void TogglePlugins()
        => _arePluginsShown = !_arePluginsShown;

    /// <summary>How many commands a row's items amount to, counting through a group rather than as one.</summary>
    private static int CountLeaves(IEnumerable<FormaToolbarItem> items)
        => items.Sum(item => item is FormaToolbarGroup group ? group.Items.Count : 1);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Editor);

        _layout = View is null
            ? ComposerToolbarLayout.Create(Editor, Plugins)
            : ComposerToolbarLayout.CreateForView(View, Plugins);
    }
}
