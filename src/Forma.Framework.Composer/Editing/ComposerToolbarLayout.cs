namespace Forma.Framework.Composer.Editing;

using Forma.Framework.Composer.Plugins;
using Forma.Framework.Toolbar;

/// <summary>One resolved command row: its identity and the items placed on it.</summary>
/// <param name="Id">The row's identifier.</param>
/// <param name="Label">The row's accessible name, in English, used as the localization key.</param>
/// <param name="IsBuiltIn">Whether this is the shared row the composer's own commands occupy.</param>
/// <param name="Items">The items placed on this row, in the order their contributors were merged.</param>
public sealed record ComposerToolbarRowLayout(
    string Id,
    string Label,
    bool IsBuiltIn,
    IReadOnlyList<FormaToolbarItem> Items);

/// <summary>The ordered command rows one surface's toolbar renders.</summary>
public sealed class ComposerToolbarLayout
{
    private ComposerToolbarLayout(IReadOnlyList<ComposerToolbarRowLayout> rows)
    {
        Rows = rows;
    }

    /// <summary>The rows to render, the shared built-in row first and each declared row after the one that defaults to it.</summary>
    public IReadOnlyList<ComposerToolbarRowLayout> Rows { get; }

    /// <summary>Lays out the editor's own commands and those of the plugins scoped to it.</summary>
    public static ComposerToolbarLayout Create(IComposerEditorPlugin editor, IEnumerable<IComposerPlugin> plugins)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(plugins);

        return Create(editor, editor.Id, plugins, StandardRows);
    }

    /// <summary>Lays out the view's own commands and those of the plugins scoped to its editor; an item naming a row the view never declared is left out.</summary>
    public static ComposerToolbarLayout CreateForView(IComposerViewPlugin view, IEnumerable<IComposerPlugin> plugins)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(plugins);

        // A view takes only the plugins that say they belong on a view; the rest act on the document, which a view never edits.
        return Create(view, view.EditorId, plugins.Where(plugin => plugin is IComposerFeaturePlugin { AppliesToViews: true }), []);
    }

    private static readonly IReadOnlyList<ComposerToolbarRow> StandardRows =
    [
        new() { Id = ComposerRows.Formatting, Label = "Text Formatting" },
        new() { Id = ComposerRows.Paragraph, Label = "Paragraph Formatting" },
        new() { Id = ComposerRows.Plugins, Label = "Plugin Commands" },
        new() { Id = ComposerRows.Selection, Label = "Selection Formatting" },
    ];

    private static ComposerToolbarLayout Create(
        IComposerPlugin owner,
        string editorId,
        IEnumerable<IComposerPlugin> plugins,
        IReadOnlyList<ComposerToolbarRow> standardRows)
    {
        var external = plugins.Where(p => !ReferenceEquals(p, owner) && p is not IComposerEditorPlugin and not IComposerViewPlugin && AppliesTo(p, editorId)).ToList();
        var order = Order(owner, external, standardRows);
        var items = order.ToDictionary(r => r.Id, _ => new List<FormaToolbarItem>(), StringComparer.Ordinal);
        var catchesStrays = items.ContainsKey(ComposerRows.Plugins);

        Place(owner.Items);

        foreach (var plugin in external)
        {
            Place(plugin.Items);
        }

        var rows = order
            .Select(r => new ComposerToolbarRowLayout(r.Id, r.Label, r.Id == ComposerRows.BuiltIn, Coalesce(items[r.Id])))
            .Where(r => r.IsBuiltIn || r.Items.Count > 0)
            .ToList();

        return new ComposerToolbarLayout(rows);

        void Place(IReadOnlyList<ComposerToolbarItem> contributed)
        {
            foreach (var entry in contributed)
            {
                if (items.TryGetValue(entry.RowId, out var row))
                {
                    row.AddRange(entry.Items);
                }
                else if (catchesStrays)
                {
                    // An entry naming a row nobody declared keeps its items reachable on the plugins row instead.
                    items[ComposerRows.Plugins].AddRange(entry.Items);
                }
            }
        }
    }

    /// <summary>Folds groups sharing an identifier into the first of them, so one named cluster holds every contribution to it.</summary>
    private static List<FormaToolbarItem> Coalesce(List<FormaToolbarItem> placed)
    {
        var merged = new List<FormaToolbarItem>(placed.Count);
        var groupIndexes = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var item in placed)
        {
            if (item is not FormaToolbarGroup group)
            {
                merged.Add(item);

                continue;
            }

            if (groupIndexes.TryGetValue(group.Id, out var index))
            {
                var existing = (FormaToolbarGroup)merged[index];
                merged[index] = existing with { Items = [.. existing.Items, .. group.Items] };

                continue;
            }

            groupIndexes[group.Id] = merged.Count;
            merged.Add(group);
        }

        return merged;
    }

    private static List<ComposerToolbarRow> Order(IComposerPlugin owner, IEnumerable<IComposerPlugin> external, IReadOnlyList<ComposerToolbarRow> standardRows)
    {
        var order = new List<ComposerToolbarRow> { new() { Id = ComposerRows.BuiltIn, Label = "Editor Commands" } };
        var seen = new HashSet<string>(StringComparer.Ordinal) { ComposerRows.BuiltIn };

        foreach (var row in standardRows)
        {
            seen.Add(row.Id);
        }

        AddDeclared(owner.Rows);
        order.AddRange(standardRows);

        // Only the editor's own surface takes rows a feature plugin declares; a view lays out what it declared itself.
        if (standardRows.Count > 0)
        {
            foreach (var plugin in external)
            {
                AddDeclared(plugin.Rows);
            }
        }

        return order;

        void AddDeclared(IReadOnlyList<ComposerToolbarRow> declared)
        {
            foreach (var row in declared.Where(r => seen.Add(r.Id)))
            {
                order.Add(row);
            }
        }
    }

    // Only a feature plugin scopes itself to editors; anything else contributes to every editor.
    private static bool AppliesTo(IComposerPlugin plugin, string editorId)
        => plugin is not IComposerFeaturePlugin feature
            || feature.SupportedEditorIds.Count == 0
            || feature.SupportedEditorIds.Contains(editorId, StringComparer.Ordinal);
}
