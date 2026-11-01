namespace Forma.Blazor.Components.Composer.Dialogs;

using System.Globalization;

using Forma.Blazor.Components.Feedback;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Input;
using Forma.Framework.Localization;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;

/// <summary>The dialog listing the shortcuts the surface answers, each under the name of the command it runs.</summary>
public sealed partial class FormaComposerShortcutsDialog : ComposerDialogBase
{
    private static readonly DialogOptions CloseOnly = new() { ShowCancelAction = false };

    /// <summary>The editor whose surface the shortcuts belong to.</summary>
    [Parameter, EditorRequired] public IComposerEditorPlugin Editor { get; set; } = default!;

    /// <summary>The view showing over the editor's document, where null means the editor itself shows.</summary>
    [Parameter] public IComposerViewPlugin? View { get; set; }

    /// <summary>The plugins in play on the surface.</summary>
    [Parameter] public IReadOnlyList<IComposerPlugin> Plugins { get; set; } = [];

    /// <summary>The shortcuts the surface answers.</summary>
    [Parameter] public ComposerKeyBindingCatalog KeyBindings { get; set; } = ComposerKeyBindingCatalog.Empty;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    [Inject] private InputRouterOptions InputRouterOptions { get; set; } = default!;

    private IReadOnlyList<ShortcutRow> _rows = [];
    private CultureInfo? _rowsUICulture;

    private ComposerPluginLocalizer Localizer
        => field ??= new ComposerPluginLocalizer(Services.GetStringLocaleFactory(), L);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Editor);
        ArgumentNullException.ThrowIfNull(Plugins);
        ArgumentNullException.ThrowIfNull(KeyBindings);

        base.OnParametersSet();
    }

    // The rows are built as the dialog opens, since the modal holds the surface while it is open, and again for a UI culture
    // other than the one they were built in; a dialog never opened has none.
    private IReadOnlyList<ShortcutRow> Rows
    {
        get
        {
            if (_rowsUICulture is not null && !Equals(_rowsUICulture, Context.CultureState.UICulture))
            {
                RefreshRows();
            }

            return _rows;
        }
    }

    /// <inheritdoc />
    private protected override void OnOpen()
        => RefreshRows();

    private void RefreshRows()
    {
        _rows = BuildRows();
        _rowsUICulture = Context.CultureState.UICulture;
    }

    // A command is titled as the toolbar titles it, and undo and redo as the composer does; a binding whose command no
    // item titles, or one a host's preemptive route claims, is left out.
    private List<ShortcutRow> BuildRows()
    {
        var titles = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ComposerCommands.Undo] = L["Undo"].Value,
            [ComposerCommands.Redo] = L["Redo"].Value,
        };

        var layout = View is null
            ? ComposerToolbarLayout.Create(Editor, Plugins, Localizer.Localize)
            : ComposerToolbarLayout.CreateForView(View, Plugins, Localizer.Localize);

        foreach (var item in layout.Rows.SelectMany(row => row.Items))
        {
            Collect(item, titles);
        }

        List<ShortcutRow> rows = [];

        foreach (var command in KeyBindings.Bindings.GroupBy(binding => binding.Command, StringComparer.Ordinal))
        {
            var reachable = command.Where(binding => !binding.IsClaimedBy(InputRouterOptions)).ToList();

            if (reachable.Count > 0 && titles.TryGetValue(command.Key, out var title))
            {
                rows.Add(new ShortcutRow(title, [.. reachable.Select(binding => binding.DisplayKeys)]));
            }
        }

        return rows;
    }

    private static void Collect(ToolbarItem item, Dictionary<string, string> titles)
    {
        switch (item)
        {
            case ToolbarGroup group:
                foreach (var child in group.Items)
                {
                    Collect(child, titles);
                }

                break;

            case ToolbarAction action:
                titles.TryAdd(action.Command, action.Title);
                break;

            case ToolbarMenu menu:
                foreach (var option in menu.Options)
                {
                    titles.TryAdd(option.Command, option.Title);
                }

                break;
        }
    }

    /// <summary>One command and every combination that runs it.</summary>
    private sealed record ShortcutRow(string Title, IReadOnlyList<IReadOnlyList<string>> Combinations);
}
