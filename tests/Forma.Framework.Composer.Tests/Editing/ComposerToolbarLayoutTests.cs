namespace Forma.Framework.Composer.Tests.Editing;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

using Xunit;

public sealed class ComposerToolbarLayoutTests
{
    private sealed class Editor : IComposerEditorPlugin
    {
        public string Id { get; init; } = "editor";
        public string Title => Id;
        public Icon Icon => Glyph.File;
        public ComposerDocumentFormat Format => ComposerDocumentFormat.PlainText;
        public Type EditorComponent => typeof(Editor);
        public IReadOnlyList<ComposerToolbarItem> Items { get; init; } = [];
        public IReadOnlyList<ComposerToolbarRow> Rows { get; init; } = [];
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    private sealed class Plugin : IComposerFeaturePlugin
    {
        public string Id { get; init; } = "plugin";
        public IReadOnlyList<ComposerToolbarItem> Items { get; init; } = [];
        public IReadOnlyList<ComposerToolbarRow> Rows { get; init; } = [];
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    private sealed class View : IComposerViewPlugin
    {
        public string Id { get; init; } = "editor.preview";
        public string Kind => ComposerViews.Preview;
        public string Title => Id;
        public Icon Icon => Glyph.Preview;
        public Type ViewComponent => typeof(View);
        public IReadOnlyList<ComposerToolbarItem> Items { get; init; } = [];
        public IReadOnlyList<ComposerToolbarRow> Rows { get; init; } = [];
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    private static ComposerToolbarItem CreateRow(string rowId, params FormaToolbarItem[] items)
        => new(rowId, items);

    [Fact]
    public void Should_lay_a_view_out_from_the_built_in_row_and_its_own_rows_alone()
    {
        // Arrange
        var view = new View
        {
            Rows = [new() { Id = "preview.page", Label = "Page" }],
            Items = [CreateRow("preview.page", CreateCommand("paper"))],
        };
        // A view is handed what is registered on it or at the composer root, so what reaches the layout is already the scope in play.
        var plugins = new IComposerPlugin[]
        {
            new Plugin { Id = "chrome", Items = [CreateRow(ComposerRows.BuiltIn, CreateCommand("maximize"))] },
            new Plugin { Id = "bold", Items = [CreateRow(ComposerRows.Formatting, CreateCommand("bold"))] },
            new Plugin { Id = "export", Items = [CreateRow("preview.page", CreateCommand("export"))] },
            new Plugin { Id = "stray", Items = [CreateRow("nowhere", CreateCommand("stray"))], Rows = [new() { Id = "declared", Label = "Declared" }] },
        };

        // Act
        var layout = ComposerToolbarLayout.CreateForView(view, plugins);

        // Assert
        Assert.Multiple(
            () => Assert.Equal([ComposerRows.BuiltIn, "preview.page"], layout.Rows.Select(row => row.Id)),
            () => Assert.Equal(["maximize"], GetItemIds(layout.Rows[0])),
            () => Assert.Equal(["paper", "export"], GetItemIds(layout.Rows[1])));
    }

    [Fact]
    public void Should_leave_a_views_items_off_its_editors_own_toolbar()
    {
        // Arrange
        var editor = new Editor();
        var view = new View { Rows = [new() { Id = "preview.page", Label = "Page" }], Items = [CreateRow("preview.page", CreateCommand("paper"))] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, [view]);

        // Assert
        Assert.DoesNotContain(layout.Rows, row => row.Id == "preview.page" || GetItemIds(row).Contains("paper"));
    }

    private static FormaToolbarCommand CreateCommand(string id)
        => new() { Id = id, Title = id, Command = id };

    private static FormaToolbarGroup Group(string id, params FormaToolbarItem[] items)
        => new() { Id = id, Title = id, Items = items };

    private static IEnumerable<string> GetItemIds(ComposerToolbarRowLayout row)
        => row.Items.SelectMany(Flatten).Select(i => i.Id);

    private static IEnumerable<FormaToolbarItem> Flatten(FormaToolbarItem item)
        => item is FormaToolbarGroup group ? group.Items.SelectMany(Flatten) : [item];

    [Fact]
    public void Should_always_render_the_built_in_row_even_when_nothing_fills_it()
    {
        // Arrange
        var editor = new Editor();

        // Act
        var layout = ComposerToolbarLayout.Create(editor, []);

        // Assert
        var row = Assert.Single(layout.Rows);
        Assert.True(row.IsBuiltIn);
    }

    [Fact]
    public void Should_put_the_editors_commands_in_the_row_it_names()
    {
        // Arrange
        var editor = new Editor { Items = [CreateRow(ComposerRows.BuiltIn, CreateCommand("bold"))] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, []);

        // Assert
        Assert.Equal(["bold"], GetItemIds(layout.Rows[0]));
    }

    [Fact]
    public void Should_put_an_external_plugins_commands_in_the_row_it_names()
    {
        // Arrange
        var editor = new Editor();
        var plugin = new Plugin { Items = [CreateRow(ComposerRows.Plugins, CreateCommand("shout"))] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, [plugin]);

        // Assert
        Assert.Multiple(
            () => Assert.Equal([ComposerRows.BuiltIn, ComposerRows.Plugins], layout.Rows.Select(r => r.Id)),
            () => Assert.Equal(["shout"], GetItemIds(layout.Rows[1])));
    }

    [Fact]
    public void Should_let_an_editor_declare_a_row_of_its_own()
    {
        // Arrange
        var editor = new Editor
        {
            Rows = [new() { Id = "format", Label = "Formatting" }],
            Items = [CreateRow("format", CreateCommand("bold"))],
        };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, []);

        // Assert
        Assert.Multiple(
            () => Assert.Equal([ComposerRows.BuiltIn, "format"], layout.Rows.Select(r => r.Id)),
            () => Assert.Equal(["bold"], GetItemIds(layout.Rows[1])));
    }

    [Fact]
    public void Should_let_an_editor_put_a_command_in_the_shared_row_alongside_one_in_its_own()
    {
        // Arrange
        var editor = new Editor
        {
            Rows = [new() { Id = "format", Label = "Formatting" }],
            Items = [CreateRow(ComposerRows.BuiltIn, CreateCommand("save")), CreateRow("format", CreateCommand("bold"))],
        };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, []);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["save"], GetItemIds(layout.Rows[0])),
            () => Assert.Equal(["bold"], GetItemIds(layout.Rows[1])));
    }

    [Fact]
    public void Should_let_a_plugin_declare_a_row_of_its_own()
    {
        // Arrange
        var editor = new Editor();
        var plugin = new Plugin
        {
            Rows = [new() { Id = "tables", Label = "Tables" }],
            Items = [CreateRow("tables", CreateCommand("insert-table"))],
        };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, [plugin]);

        // Assert
        Assert.Multiple(
            () => Assert.Equal([ComposerRows.BuiltIn, "tables"], layout.Rows.Select(r => r.Id)),
            () => Assert.Equal(["insert-table"], GetItemIds(layout.Rows[1])));
    }

    [Fact]
    public void Should_order_editor_rows_before_the_plugin_row_and_plugin_rows_after_it()
    {
        // Arrange
        var editor = new Editor
        {
            Rows = [new() { Id = "format", Label = "Formatting" }],
            Items = [CreateRow("format", CreateCommand("bold"))],
        };
        var plugin = new Plugin
        {
            Rows = [new() { Id = "tables", Label = "Tables" }],
            Items = [CreateRow(ComposerRows.Plugins, CreateCommand("shout")), CreateRow("tables", CreateCommand("insert-table"))],
        };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, [plugin]);

        // Assert
        Assert.Equal([ComposerRows.BuiltIn, "format", ComposerRows.Plugins, "tables"], layout.Rows.Select(r => r.Id));
    }

    [Fact]
    public void Should_drop_a_declared_row_nothing_fills()
    {
        // Arrange
        var editor = new Editor { Rows = [new() { Id = "format", Label = "Formatting" }] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, []);

        // Assert
        Assert.Equal([ComposerRows.BuiltIn], layout.Rows.Select(r => r.Id));
    }

    [Fact]
    public void Should_fall_back_to_the_plugins_row_when_an_item_names_a_row_nobody_declared()
    {
        // Arrange
        var editor = new Editor { Items = [CreateRow("nowhere", CreateCommand("bold"))] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, []);

        // Assert
        Assert.Multiple(
            () => Assert.Equal([ComposerRows.BuiltIn, ComposerRows.Plugins], layout.Rows.Select(r => r.Id)),
            () => Assert.Equal(["bold"], GetItemIds(layout.Rows[1])));
    }

    [Fact]
    public void Should_translate_each_contribution_in_the_language_of_the_plugin_that_declared_it()
    {
        // Arrange
        var editor = new Editor
        {
            Rows = [new() { Id = "editor.row", Label = "Editor Row" }],
            Items = [CreateRow("editor.row", CreateCommand("own"))],
        };
        var first = new Plugin { Id = "first", Items = [CreateRow(ComposerRows.Formatting, Group("media", CreateCommand("image")))] };
        var second = new Plugin { Id = "second", Items = [CreateRow(ComposerRows.Formatting, Group("media", CreateCommand("browse")))] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, [first, second], (plugin, title) => $"{plugin?.Id ?? "composer"}:{title}");

        // Assert
        var formatting = layout.Rows.Single(row => row.Id == ComposerRows.Formatting);
        var media = Assert.IsType<FormaToolbarGroup>(Assert.Single(formatting.Items));
        Assert.Multiple(
            () => Assert.Equal("composer:Editor Commands", layout.Rows[0].Label),
            () => Assert.Equal("editor:Editor Row", layout.Rows.Single(row => row.Id == "editor.row").Label),
            () => Assert.Equal("composer:Text Formatting", formatting.Label),
            () => Assert.Equal("first:media", media.Title),
            () => Assert.Equal(["first:image", "second:browse"], media.Items.Select(item => item.Title)));
    }

    [Fact]
    public void Should_keep_the_first_declaration_when_two_plugins_declare_the_same_row()
    {
        // Arrange
        var editor = new Editor();
        var first = new Plugin { Id = "a", Rows = [new() { Id = "shared", Label = "First" }], Items = [CreateRow("shared", CreateCommand("one"))] };
        var second = new Plugin { Id = "b", Rows = [new() { Id = "shared", Label = "Second" }], Items = [CreateRow("shared", CreateCommand("two"))] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, [first, second]);

        // Assert
        var shared = Assert.Single(layout.Rows, r => r.Id == "shared");
        Assert.Multiple(
            () => Assert.Equal("First", shared.Label),
            () => Assert.Equal(["one", "two"], GetItemIds(shared)));
    }

    [Fact]
    public void Should_fold_two_plugins_contributions_into_one_group_when_both_name_it()
    {
        // Arrange
        var editor = new Editor();
        var first = new Plugin { Id = "a", Items = [CreateRow(ComposerRows.BuiltIn, Group("view", CreateCommand("preview")))] };
        var second = new Plugin { Id = "b", Items = [CreateRow(ComposerRows.BuiltIn, Group("view", CreateCommand("maximize")))] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, [first, second]);

        // Assert
        var group = Assert.IsType<FormaToolbarGroup>(Assert.Single(layout.Rows[0].Items));
        Assert.Multiple(
            () => Assert.Equal("view", group.Id),
            () => Assert.Equal(["preview", "maximize"], group.Items.Select(i => i.Id)));
    }

    [Fact]
    public void Should_keep_groups_apart_when_they_name_different_clusters()
    {
        // Arrange
        var editor = new Editor();
        var plugin = new Plugin { Items = [CreateRow(ComposerRows.BuiltIn, Group("view", CreateCommand("preview")), Group("insert", CreateCommand("rule")))] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, [plugin]);

        // Assert
        Assert.Equal(["view", "insert"], layout.Rows[0].Items.Select(i => i.Id));
    }
}
