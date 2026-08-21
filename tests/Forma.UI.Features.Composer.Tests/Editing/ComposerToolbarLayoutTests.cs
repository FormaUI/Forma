namespace Forma.UI.Features.Composer.Tests.Editing;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Documents;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Style.Glyphs;

using Xunit;

public sealed class ComposerToolbarLayoutTests
{
    private sealed class Editor : IComposerEditorPlugin
    {
        public string Id { get; init; } = "editor";
        public string Title => Id;
        public Glyph Icon => Glyph.File;
        public ComposerDocumentFormat Format => ComposerDocumentFormat.PlainText;
        public Type EditorComponent => typeof(Editor);
        public IReadOnlyList<ComposerToolbarItem> ToolbarItems { get; init; } = [];
        public IReadOnlyList<ComposerToolbarRow> ToolbarRows { get; init; } = [];
        public IReadOnlyList<ComposerKeyBinding> KeyBindings => [];
        public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];
        public IReadOnlyList<ComposerAsset> Assets => [];
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    private sealed class Plugin : IComposerPlugin
    {
        public string Id { get; init; } = "plugin";
        public IReadOnlyList<ComposerToolbarItem> ToolbarItems { get; init; } = [];
        public IReadOnlyList<ComposerToolbarRow> ToolbarRows { get; init; } = [];
        public IReadOnlyList<string> SupportedEditorIds { get; init; } = [];
        public IReadOnlyList<ComposerKeyBinding> KeyBindings => [];
        public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];
        public IReadOnlyList<ComposerAsset> Assets => [];
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
    }

    private static ComposerToolbarItem Item(string id, string? row = null)
        => new() { Id = id, Title = id, Command = id, Row = row };

    private static IEnumerable<string> ItemIds(ComposerToolbarRowLayout row)
        => row.Groups.SelectMany(g => g).Select(i => i.Id);

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
    public void Should_put_the_editors_unrouted_commands_in_the_built_in_row()
    {
        // Arrange
        var editor = new Editor { ToolbarItems = [Item("bold")] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, []);

        // Assert
        Assert.Equal(["bold"], ItemIds(layout.Rows[0]));
    }

    [Fact]
    public void Should_put_an_external_plugins_unrouted_commands_in_the_plugin_row()
    {
        // Arrange
        var editor = new Editor();
        var plugin = new Plugin { ToolbarItems = [Item("shout")] };

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
            ToolbarRows = [new() { Id = "format", Label = "Formatting" }],
            ToolbarItems = [Item("bold", row: "format")],
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
            ToolbarRows = [new() { Id = "format", Label = "Formatting" }],
            ToolbarItems = [Item("save"), Item("bold", row: "format")],
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
            ToolbarRows = [new() { Id = "tables", Label = "Tables" }],
            ToolbarItems = [Item("insert-table", row: "tables")],
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
            ToolbarRows = [new() { Id = "format", Label = "Formatting" }],
            ToolbarItems = [Item("bold", row: "format")],
        };
        var plugin = new Plugin
        {
            ToolbarRows = [new() { Id = "tables", Label = "Tables" }],
            ToolbarItems = [Item("shout"), Item("insert-table", row: "tables")],
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
        var editor = new Editor { ToolbarRows = [new() { Id = "format", Label = "Formatting" }] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, []);

        // Assert
        Assert.Equal([ComposerRows.BuiltIn], layout.Rows.Select(r => r.Id));
    }

    [Fact]
    public void Should_fall_back_to_the_default_row_when_an_item_names_a_row_nobody_declared()
    {
        // Arrange
        var editor = new Editor { ToolbarItems = [Item("bold", row: "nowhere")] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, []);

        // Assert
        Assert.Multiple(
            () => Assert.Equal([ComposerRows.BuiltIn], layout.Rows.Select(r => r.Id)),
            () => Assert.Equal(["bold"], ItemIds(layout.Rows[0])));
    }

    [Fact]
    public void Should_leave_out_a_plugin_scoped_to_another_editor()
    {
        // Arrange
        var editor = new Editor { Id = "plain" };
        var plugin = new Plugin { SupportedEditorIds = ["rich"], ToolbarItems = [Item("shout")] };

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
        var first = new Plugin { Id = "a", ToolbarRows = [new() { Id = "shared", Label = "First" }], ToolbarItems = [Item("one", row: "shared")] };
        var second = new Plugin { Id = "b", ToolbarRows = [new() { Id = "shared", Label = "Second" }], ToolbarItems = [Item("two", row: "shared")] };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, [first, second]);

        // Assert
        var shared = Assert.Single(layout.Rows, r => r.Id == "shared");
        Assert.Multiple(
            () => Assert.Equal("First", shared.Label),
            () => Assert.Equal(["one", "two"], ItemIds(shared)));
    }

    [Fact]
    public void Should_separate_commands_into_groups_within_a_row()
    {
        // Arrange
        var editor = new Editor
        {
            ToolbarItems =
            [
                new() { Id = "bold", Title = "bold", Group = "text" },
                new() { Id = "link", Title = "link", Group = "insert" },
            ],
        };

        // Act
        var layout = ComposerToolbarLayout.Create(editor, []);

        // Assert
        Assert.Equal(2, layout.Rows[0].Groups.Count);
    }
}
