namespace Forma.Blazor.Components.Composer;

using System.Globalization;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Localization;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;

/// <summary>The toolbar of an editor or a view over it, with rows for its own commands and its plugins' commands.</summary>
public sealed partial class FormaComposerToolbar : FormaComponentBase
{
    /// <summary>The most commands one bar carries.</summary>
    public const int CommandsPerBar = 12;

    /// <summary>The editor this toolbar belongs to, or whose view it belongs to.</summary>
    [Parameter, EditorRequired] public IComposerEditorPlugin Editor { get; set; } = default!;

    /// <summary>The view this toolbar belongs to, where null means it belongs to the editor itself.</summary>
    [Parameter] public IComposerViewPlugin? View { get; set; }

    /// <summary>The feature plugins in play on this bar's surface.</summary>
    [Parameter] public IReadOnlyList<IComposerPlugin> Plugins { get; set; } = [];

    /// <summary>Whether every control on the toolbar is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The key bindings each action's shortcut is read from.</summary>
    [Parameter] public ComposerKeyBindingCatalog KeyBindings { get; set; } = ComposerKeyBindingCatalog.Empty;

    /// <summary>Callback invoked with the command an action raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    [Inject] private IServiceProvider Services { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    private ComposerToolbarLayout _layout = default!;
    private LayoutInputs? _layoutInputs;
    private IReadOnlyList<ComposerToolbarRowLayout> _bars = [];
    private IReadOnlyList<ToolbarItem> _pluginItems = [];
    private bool _arePluginsShown;

    private ComposerPluginLocalizer Localizer
        => field ??= new ComposerPluginLocalizer(Services.GetStringLocaleFactory(), L);

    private string PluginsId
        => $"composer-plugins-{Context.RuntimeId:N}";

    /// <summary>The plugin whose bar this is: the view when one shows, and otherwise the editor.</summary>
    private IComposerPlugin Owner
        => View ?? (IComposerPlugin)Editor;

    /// <summary>The bars to render, with small rows folded together.</summary>
    private IReadOnlyList<ComposerToolbarRowLayout> Bars
        => _bars;

    private IReadOnlyList<ToolbarItem> PluginItems
        => _pluginItems;

    private IEnumerable<ComposerToolbarRowLayout> EditorRows
        => _layout.Rows.Where(row => row.Id is not (ComposerRows.Plugins or ComposerRows.Selection));

    private void TogglePlugins()
        => _arePluginsShown = !_arePluginsShown;

    /// <summary>The number of commands in the items, each item of a group counted.</summary>
    private static int CountLeaves(IEnumerable<ToolbarItem> items)
        => items.Sum(item => item is ToolbarGroup group ? group.Items.Count : 1);

    // The layout is rebuilt only when its inputs change.
    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Editor);

        var inputs = new LayoutInputs(Editor, View, Plugins, Context.CultureState.UICulture.Name);

        if (inputs == _layoutInputs)
        {
            return;
        }

        _layoutInputs = inputs;
        _layout = View is null
            ? ComposerToolbarLayout.Create(Editor, Plugins, Localizer.Localize)
            : ComposerToolbarLayout.CreateForView(View, Plugins, Localizer.Localize);
        _bars = FoldBars();
        _pluginItems = _layout.Rows.FirstOrDefault(row => row.Id == ComposerRows.Plugins)?.Items ?? [];
    }

    private List<ComposerToolbarRowLayout> FoldBars()
    {
        var bars = new List<ComposerToolbarRowLayout>();

        foreach (var row in EditorRows.Where(row => row.Items.Count > 0))
        {
            var count = CountLeaves(row.Items);
            var lastBar = bars.Count > 0 ? bars[^1] : null;

            // Folding coalesces groups of the same name across the joined rows.
            if (lastBar is not null && CountLeaves(lastBar.Items) + count <= CommandsPerBar)
            {
                bars[^1] = lastBar with { Items = ComposerToolbarLayout.Coalesce([.. lastBar.Items, .. row.Items]) };
            }
            else
            {
                bars.Add(row);
            }
        }

        return bars.Count > 0 ? bars : [_layout.Rows.First(row => row.IsBuiltIn)];
    }

    /// <summary>What the layout is built from.</summary>
    /// <remarks>The plugins compare by identity, as the composer hands the same instances over across renders.</remarks>
    private sealed record LayoutInputs(IComposerEditorPlugin Editor, IComposerViewPlugin? View, IReadOnlyList<IComposerPlugin> Plugins, string Culture);
}
