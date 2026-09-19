namespace Forma.Framework.Composer.Editing;

using Forma.Framework.Composer.Plugins;
using Forma.Framework.Toolbar;

/// <summary>One resolved command row: its identity and the items placed on it.</summary>
/// <param name="Id">The row's identifier.</param>
/// <param name="Label">The row's accessible name, localized when the layout was given a way to localize it.</param>
/// <param name="IsBuiltIn">Whether this is the shared row the composer's own commands occupy.</param>
/// <param name="Items">The items placed on this row, in the order their contributors were merged.</param>
public sealed record ComposerToolbarRowLayout(
    string Id,
    string Label,
    bool IsBuiltIn,
    IReadOnlyList<ToolbarItem> Items);

/// <summary>The ordered command rows one surface's toolbar renders.</summary>
public sealed class ComposerToolbarLayout
{
    private ComposerToolbarLayout(IReadOnlyList<ComposerToolbarRowLayout> rows)
    {
        Rows = rows;
    }

    /// <summary>The rows to render, the shared built-in row first and each declared row after the one that defaults to it.</summary>
    public IReadOnlyList<ComposerToolbarRowLayout> Rows { get; }

    /// <summary>Lays out the editor's own commands and those of the plugins it is handed.</summary>
    /// <param name="editor">The editor whose toolbar this is.</param>
    /// <param name="plugins">The capabilities in play on the editor's surface.</param>
    /// <param name="localize">
    /// Reads an English title in the reader's language, for the plugin that declared it or for the composer when the plugin is null;
    /// omitted, every title is laid out as declared.
    /// </param>
    public static ComposerToolbarLayout Create(
        IComposerEditorPlugin editor,
        IEnumerable<IComposerPlugin> plugins,
        Func<IComposerPlugin?, string, string>? localize = null)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(plugins);

        return Create(editor, plugins, StandardRows, localize ?? KeepTitle);
    }

    /// <summary>Lays out the view's own commands and those of the plugins it is handed; an item naming a row the view never declared is left out.</summary>
    /// <param name="view">The view whose bar this is.</param>
    /// <param name="plugins">The capabilities in play on the view.</param>
    /// <param name="localize">
    /// Reads an English title in the reader's language, for the plugin that declared it or for the composer when the plugin is null;
    /// omitted, every title is laid out as declared.
    /// </param>
    public static ComposerToolbarLayout CreateForView(
        IComposerViewPlugin view,
        IEnumerable<IComposerPlugin> plugins,
        Func<IComposerPlugin?, string, string>? localize = null)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(plugins);

        return Create(view, plugins, [], localize ?? KeepTitle);
    }

    private static readonly IReadOnlyList<ComposerToolbarRow> StandardRows =
    [
        new() { Id = ComposerRows.Formatting, Label = "Text Formatting" },
        new() { Id = ComposerRows.Paragraph, Label = "Paragraph Formatting" },
        new() { Id = ComposerRows.Plugins, Label = "Plugin Commands" },
        new() { Id = ComposerRows.Selection, Label = "Selection Formatting" },
    ];

    private static string KeepTitle(IComposerPlugin? plugin, string title)
        => title;

    private static ComposerToolbarLayout Create(
        IComposerPlugin owner,
        IEnumerable<IComposerPlugin> plugins,
        IReadOnlyList<ComposerToolbarRow> standardRows,
        Func<IComposerPlugin?, string, string> localize)
    {
        var external = plugins.Where(p => !ReferenceEquals(p, owner) && p is not IComposerEditorPlugin and not IComposerViewPlugin).ToList();
        var order = Order(owner, external, standardRows, localize);
        var items = order.ToDictionary(r => r.Id, _ => new List<ToolbarItem>(), StringComparer.Ordinal);
        var catchesStrays = items.ContainsKey(ComposerRows.Plugins);

        Place(owner);

        foreach (var plugin in external)
        {
            Place(plugin);
        }

        var rows = order
            .Select(r => new ComposerToolbarRowLayout(r.Id, r.Label, r.Id == ComposerRows.BuiltIn, Coalesce(items[r.Id])))
            .Where(r => r.IsBuiltIn || r.Items.Count > 0)
            .ToList();

        return new ComposerToolbarLayout(rows);

        // Each contribution is localized before groups fold together, since afterwards nothing says which plugin declared an item.
        void Place(IComposerPlugin contributor)
        {
            foreach (var entry in contributor.Items)
            {
                var localized = entry.Items.Select(item => Localize(item, title => localize(contributor, title)));

                if (items.TryGetValue(entry.RowId, out var row))
                {
                    row.AddRange(localized);
                }
                else if (catchesStrays)
                {
                    // An entry naming a row nobody declared keeps its items reachable on the plugins row instead.
                    items[ComposerRows.Plugins].AddRange(localized);
                }
            }
        }
    }

    private static ToolbarItem Localize(ToolbarItem item, Func<string, string> localize)
        => item switch
        {
            ToolbarGroup group => group with { Title = localize(group.Title), Items = [.. group.Items.Select(child => Localize(child, localize))] },
            ToolbarToggle toggle => toggle with
            {
                Title = localize(toggle.Title),
                AlternateTitle = toggle.AlternateTitle is { } alternateTitle ? localize(alternateTitle) : null,
            },
            ToolbarMenu menu => menu with
            {
                Title = localize(menu.Title),
                Options = [.. menu.Options.Select(option => option with { Title = localize(option.Title) })],
            },
            _ => item with { Title = localize(item.Title) },
        };

    /// <summary>Folds groups sharing an identifier into the first of them, so one named cluster holds every contribution to it.</summary>
    private static List<ToolbarItem> Coalesce(List<ToolbarItem> placed)
    {
        var merged = new List<ToolbarItem>(placed.Count);
        var groupIndexes = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var item in placed)
        {
            if (item is not ToolbarGroup group)
            {
                merged.Add(item);

                continue;
            }

            if (groupIndexes.TryGetValue(group.Id, out var index))
            {
                var existing = (ToolbarGroup)merged[index];
                merged[index] = existing with { Items = [.. existing.Items, .. group.Items] };

                continue;
            }

            groupIndexes[group.Id] = merged.Count;
            merged.Add(group);
        }

        return merged;
    }

    private static List<ComposerToolbarRow> Order(
        IComposerPlugin owner,
        IEnumerable<IComposerPlugin> external,
        IReadOnlyList<ComposerToolbarRow> standardRows,
        Func<IComposerPlugin?, string, string> localize)
    {
        var order = new List<ComposerToolbarRow> { LocalizeRow(new() { Id = ComposerRows.BuiltIn, Label = "Editor Commands" }, contributor: null) };
        var seen = new HashSet<string>(StringComparer.Ordinal) { ComposerRows.BuiltIn };

        foreach (var row in standardRows)
        {
            seen.Add(row.Id);
        }

        AddDeclared(owner);
        order.AddRange(standardRows.Select(row => LocalizeRow(row, contributor: null)));

        // Only the editor's own surface takes rows a feature plugin declares; a view lays out what it declared itself.
        if (standardRows.Count > 0)
        {
            foreach (var plugin in external)
            {
                AddDeclared(plugin);
            }
        }

        return order;

        void AddDeclared(IComposerPlugin contributor)
        {
            foreach (var row in contributor.Rows.Where(r => seen.Add(r.Id)))
            {
                order.Add(LocalizeRow(row, contributor));
            }
        }

        ComposerToolbarRow LocalizeRow(ComposerToolbarRow row, IComposerPlugin? contributor)
        {
            return row with { Label = localize(contributor, row.Label) };
        }
    }
}
