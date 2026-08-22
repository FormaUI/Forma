namespace Forma.UI.Features.Composer.Editing;

using Forma.UI.Features.Composer.Plugins;

/// <summary>One resolved command row: its identity and the grouped commands it carries.</summary>
/// <param name="Id">The row's identifier.</param>
/// <param name="Label">The row's accessible name, in English, used as the localization key.</param>
/// <param name="IsBuiltIn">Whether this is the shared row the composer's own commands occupy.</param>
/// <param name="Groups">The commands, grouped as their contributors grouped them.</param>
public sealed record ComposerToolbarRowLayout(
    string Id,
    string Label,
    bool IsBuiltIn,
    IReadOnlyList<IReadOnlyList<ComposerToolbarItem>> Groups);

/// <summary>The ordered command rows one editing surface's toolbar renders.</summary>
public sealed class ComposerToolbarLayout
{
    private ComposerToolbarLayout(IReadOnlyList<ComposerToolbarRowLayout> rows)
    {
        Rows = rows;
    }

    /// <summary>The rows to render, the shared built-in row first and each declared row after the one that defaults to it.</summary>
    public IReadOnlyList<ComposerToolbarRowLayout> Rows { get; }

    /// <summary>Lays out the surface's own commands and those of the plugins scoped to it.</summary>
    public static ComposerToolbarLayout Create(IComposerEditorPlugin editor, IEnumerable<IComposerPlugin> plugins)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(plugins);

        var external = plugins.Where(p => !ReferenceEquals(p, editor) && AppliesTo(p, editor.Id)).ToList();
        var order = Order(editor, external);
        var items = order.ToDictionary(r => r.Id, _ => new List<ComposerToolbarItem>(), StringComparer.Ordinal);

        Place(editor.ToolbarItems, ComposerRows.BuiltIn);

        foreach (var plugin in external)
        {
            Place(plugin.ToolbarItems, plugin.DefaultRow);
        }

        var rows = order
            .Select(r => new ComposerToolbarRowLayout(r.Id, r.Label, r.Id == ComposerRows.BuiltIn, Group(items[r.Id])))
            .Where(r => r.IsBuiltIn || r.Groups.Count > 0)
            .ToList();

        return new ComposerToolbarLayout(rows);

        void Place(IReadOnlyList<ComposerToolbarItem> contributed, string defaultRow)
        {
            foreach (var item in contributed)
            {
                // An item naming a row nobody declared keeps its command reachable in the row it defaults to.
                var row = item.Row is { } named && items.ContainsKey(named) ? named : defaultRow;
                items[row].Add(item);
            }
        }
    }

    private static List<ComposerToolbarRow> Order(IComposerEditorPlugin editor, IEnumerable<IComposerPlugin> external)
    {
        var order = new List<ComposerToolbarRow> { new() { Id = ComposerRows.BuiltIn, Label = "Editor Commands" } };
        var seen = new HashSet<string>(StringComparer.Ordinal)
        {
            ComposerRows.BuiltIn,
            ComposerRows.Formatting,
            ComposerRows.Paragraph,
            ComposerRows.Plugins,
        };

        AddDeclared(editor.ToolbarRows);
        order.Add(new() { Id = ComposerRows.Formatting, Label = "Text Formatting" });
        order.Add(new() { Id = ComposerRows.Paragraph, Label = "Paragraph Formatting" });
        order.Add(new() { Id = ComposerRows.Plugins, Label = "Plugin Commands" });

        foreach (var plugin in external)
        {
            AddDeclared(plugin.ToolbarRows);
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

    private static bool AppliesTo(IComposerPlugin plugin, string editorId)
        => plugin.SupportedEditorIds.Count == 0 || plugin.SupportedEditorIds.Contains(editorId, StringComparer.Ordinal);

    private static IReadOnlyList<IReadOnlyList<ComposerToolbarItem>> Group(List<ComposerToolbarItem> items)
        => items.Count == 0
            ? []
            : [.. items.GroupBy(i => i.Group ?? string.Empty).Select(g => (IReadOnlyList<ComposerToolbarItem>)[.. g])];
}
