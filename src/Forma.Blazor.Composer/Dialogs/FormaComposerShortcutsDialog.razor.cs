namespace Forma.Blazor.Components.Composer.Dialogs;

using Forma.Blazor.Components.Feedback;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Localization;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;

/// <summary>Dialog listing every shortcut the surface currently answers, each under the name of the command it runs.</summary>
public sealed partial class FormaComposerShortcutsDialog : ComposerDialogBase
{
    private static readonly DialogOptions CloseOnly = new() { ShowCancelAction = false };

    /// <summary>The editor whose surface the shortcuts belong to.</summary>
    [Parameter, EditorRequired] public IComposerEditorPlugin Editor { get; set; } = default!;

    /// <summary>The view showing over the editor's document, or null while the editor itself is.</summary>
    [Parameter] public IComposerViewPlugin? View { get; set; }

    /// <summary>The capabilities in play on the surface.</summary>
    [Parameter] public IReadOnlyList<IComposerPlugin> Plugins { get; set; } = [];

    /// <summary>The shortcuts the surface answers.</summary>
    [Parameter] public ComposerKeyBindingCatalog KeyBindings { get; set; } = ComposerKeyBindingCatalog.Empty;

    [Inject] private IStringLocaleFactory StringLocaleFactory { get; set; } = default!;

    private IReadOnlyList<ShortcutRow> _rows = [];

    private ComposerPluginLocalizer Localizer
        => field ??= new ComposerPluginLocalizer(StringLocaleFactory, L);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Editor);
        ArgumentNullException.ThrowIfNull(Plugins);
        ArgumentNullException.ThrowIfNull(KeyBindings);

        base.OnParametersSet();
    }

    // Nothing the rows are built from can change while a modal holds the surface, so they are built once, as it opens.
    /// <inheritdoc />
    private protected override void OnOpen()
        => _rows = BuildRows();

    // Each shortcut is named as the toolbar names its command, so what a reader hears is what they see on the button; a
    // binding no item names is left out, since nothing on the surface would tell a reader what it does. Undo and redo are
    // the command bar's own rather than any plugin's, so their names come from the composer directly.
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
            if (titles.TryGetValue(command.Key, out var title))
            {
                rows.Add(new ShortcutRow(title, [.. command.Select(binding => binding.DisplayKeys)]));
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
