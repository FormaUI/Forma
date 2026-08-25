namespace Forma.UI.Features.Composer.Tests.Editing;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Documents;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;
using Forma.UI.Style.Icons;

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
        public IReadOnlyList<ComposerKeyBinding> KeyBindings => [];
        public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];
        public IReadOnlyList<ComposerAsset> Assets => [];
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    private sealed class Plugin : IComposerPlugin
    {
        public string Id { get; init; } = "plugin";
        public IReadOnlyList<ComposerToolbarItem> Items { get; init; } = [];
        public IReadOnlyList<ComposerToolbarRow> Rows { get; init; } = [];
        public IReadOnlyList<string> SupportedEditorIds { get; init; } = [];
        public IReadOnlyList<ComposerKeyBinding> KeyBindings => [];
        public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];
        public IReadOnlyList<ComposerAsset> Assets => [];
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    private static ComposerToolbarItem Row(string rowId, params FormaToolbarItem[] items)
        => new(rowId, items);

    private static FormaToolbarCommand Command(string id)
        => new() { Id = id, Title = id, Command = id };

    private static FormaToolbarGroup Group(string id, params FormaToolbarItem[] items)
        => new() { Id = id, Title = id, Items = items };

    private static IEnumerable<string> ItemIds(ComposerToolbarRowLayout row)
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
        var editor = new Editor { Items = [Row(ComposerRows.BuiltIn, Command("bold"))] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, []);

        // Assert
        Assert.Equal(["bold"], ItemIds(layout.Rows[0]));
    }

    [Fact]
    public void Should_put_an_external_plugins_commands_in_the_row_it_names()
    {
        // Arrange
        var editor = new Editor();
        var plugin = new Plugin { Items = [Row(ComposerRows.Plugins, Command("shout"))] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, [plugin]);

        // Assert
        Assert.Multiple(
            () => Assert.Equal([ComposerRows.BuiltIn, ComposerRows.Plugins], layout.Rows.Select(r => r.Id)),
            () => Assert.Equal(["shout"], ItemIds(layout.Rows[1])));
    }

    [Fact]
    public void Should_let_an_editor_declare_a_row_of_its_own()
    {
        // Arrange
        var editor = new Editor
        {
            Rows = [new() { Id = "format", Label = "Formatting" }],
            Items = [Row("format", Command("bold"))],
        };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, []);

        // Assert
        Assert.Multiple(
            () => Assert.Equal([ComposerRows.BuiltIn, "format"], layout.Rows.Select(r => r.Id)),
            () => Assert.Equal(["bold"], ItemIds(layout.Rows[1])));
    }

    [Fact]
    public void Should_let_an_editor_put_a_command_in_the_shared_row_alongside_one_in_its_own()
    {
        // Arrange
        var editor = new Editor
        {
            Rows = [new() { Id = "format", Label = "Formatting" }],
            Items = [Row(ComposerRows.BuiltIn, Command("save")), Row("format", Command("bold"))],
        };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, []);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["save"], ItemIds(layout.Rows[0])),
            () => Assert.Equal(["bold"], ItemIds(layout.Rows[1])));
    }

    [Fact]
    public void Should_let_a_plugin_declare_a_row_of_its_own()
    {
        // Arrange
        var editor = new Editor();
        var plugin = new Plugin
        {
            Rows = [new() { Id = "tables", Label = "Tables" }],
            Items = [Row("tables", Command("insert-table"))],
        };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, [plugin]);

        // Assert
        Assert.Multiple(
            () => Assert.Equal([ComposerRows.BuiltIn, "tables"], layout.Rows.Select(r => r.Id)),
            () => Assert.Equal(["insert-table"], ItemIds(layout.Rows[1])));
    }

    [Fact]
    public void Should_order_editor_rows_before_the_plugin_row_and_plugin_rows_after_it()
    {
        // Arrange
        var editor = new Editor
        {
            Rows = [new() { Id = "format", Label = "Formatting" }],
            Items = [Row("format", Command("bold"))],
        };
        var plugin = new Plugin
        {
            Rows = [new() { Id = "tables", Label = "Tables" }],
            Items = [Row(ComposerRows.Plugins, Command("shout")), Row("tables", Command("insert-table"))],
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
        var editor = new Editor { Items = [Row("nowhere", Command("bold"))] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, []);

        // Assert
        Assert.Multiple(
            () => Assert.Equal([ComposerRows.BuiltIn, ComposerRows.Plugins], layout.Rows.Select(r => r.Id)),
            () => Assert.Equal(["bold"], ItemIds(layout.Rows[1])));
    }

    [Fact]
    public void Should_leave_out_a_plugin_scoped_to_another_editor()
    {
        // Arrange
        var editor = new Editor { Id = "plain" };
        var plugin = new Plugin { SupportedEditorIds = ["rich"], Items = [Row(ComposerRows.Plugins, Command("shout"))] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, [plugin]);

        // Assert
        Assert.Equal([ComposerRows.BuiltIn], layout.Rows.Select(r => r.Id));
    }

    [Fact]
    public void Should_keep_the_first_declaration_when_two_plugins_declare_the_same_row()
    {
        // Arrange
        var editor = new Editor();
        var first = new Plugin { Id = "a", Rows = [new() { Id = "shared", Label = "First" }], Items = [Row("shared", Command("one"))] };
        var second = new Plugin { Id = "b", Rows = [new() { Id = "shared", Label = "Second" }], Items = [Row("shared", Command("two"))] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, [first, second]);

        // Assert
        var shared = Assert.Single(layout.Rows, r => r.Id == "shared");
        Assert.Multiple(
            () => Assert.Equal("First", shared.Label),
            () => Assert.Equal(["one", "two"], ItemIds(shared)));
    }

    [Fact]
    public void Should_fold_two_plugins_contributions_into_one_group_when_both_name_it()
    {
        // Arrange
        var editor = new Editor();
        var first = new Plugin { Id = "a", Items = [Row(ComposerRows.BuiltIn, Group("view", Command("preview")))] };
        var second = new Plugin { Id = "b", Items = [Row(ComposerRows.BuiltIn, Group("view", Command("maximize")))] };

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
        var plugin = new Plugin { Items = [Row(ComposerRows.BuiltIn, Group("view", Command("preview")), Group("insert", Command("rule")))] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, [plugin]);

        // Assert
        Assert.Equal(["view", "insert"], layout.Rows[0].Items.Select(i => i.Id));
    }
}
