namespace Forma.UI.Blazor.Tests.Components.Composer.RichTextEditor;

using Forma.UI.Blazor.Components.Composer.RichTextEditor;
using Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;
using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Documents;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Composer.Sanitization;
using Forma.UI.Features.Toolbar;

public sealed class RichTextPluginTests
{
    private static readonly NullAssetLoader SharedAssetLoader = new();

    private static FormaToolbarMenu FindMenu(IReadOnlyList<ComposerToolbarItem> items, string id)
        => (FormaToolbarMenu)items.SelectMany(entry => entry.Items).SelectMany(Flatten).Single(item => item.Id == id);

    private static IEnumerable<FormaToolbarItem> Flatten(FormaToolbarItem item)
        => item is FormaToolbarGroup group ? group.Items.SelectMany(Flatten) : [item];

    private static ComposerContext CreateContext(string html = "<p>Test</p>")
    {
        var doc = new ComposerDocument(ComposerDocumentFormat.Html, html);
        var history = new ComposerHistory();
        var regions = new ComposerRegionRegistry();
        return new ComposerContext(
            "rich-text",
            doc,
            ComposerSelection.Empty,
            history,
            new DefaultHtmlSanitizer(),
            SharedAssetLoader,
            regions,
            _ => Task.CompletedTask,
            () => Task.CompletedTask,
            _ => Task.CompletedTask);
    }

    [Fact]
    public async Task BasicFormattingPlugin_should_dispatch_formatting_commands()
    {
        // Arrange
        var editor = new RichTextEditorPlugin();
        var plugin = new BasicFormattingPlugin(editor);
        var context = CreateContext();
        string? requestedCommand = null;
        editor.CommandRequested += (cmd, _) => requestedCommand = cmd;

        // Act
        await plugin.ExecuteAsync(BasicFormattingPlugin.BoldCommand, context, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("bold", requestedCommand);
        Assert.Equal("basic-formatting", plugin.Id);
        Assert.NotEmpty(plugin.Items);
        Assert.NotEmpty(plugin.KeyBindings);
    }

    [Fact]
    public async Task ParagraphFormatPlugin_should_dispatch_block_commands()
    {
        // Arrange
        var editor = new RichTextEditorPlugin();
        var plugin = new ParagraphFormatPlugin(editor);
        var context = CreateContext();
        string? requestedCommand = null;
        object? requestedValue = null;
        editor.CommandRequested += (cmd, val) =>
        {
            requestedCommand = cmd;
            requestedValue = val;
        };

        // Act
        await plugin.ExecuteAsync(ParagraphFormatPlugin.Heading1Command, context, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("formatBlock", requestedCommand);
        Assert.Equal("h1", requestedValue);
        Assert.Equal("paragraph-format", plugin.Id);
    }

    [Fact]
    public async Task ListPlugin_should_dispatch_list_commands()
    {
        // Arrange
        var editor = new RichTextEditorPlugin();
        var plugin = new ListPlugin(editor);
        var context = CreateContext();
        string? requestedCommand = null;
        editor.CommandRequested += (cmd, _) => requestedCommand = cmd;

        // Act
        await plugin.ExecuteAsync(ListPlugin.BulletListCommand, context, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("insertUnorderedList", requestedCommand);
        Assert.Equal("list", plugin.Id);
    }

    [Fact]
    public async Task AlignmentBidiPlugin_should_dispatch_alignment_and_direction_commands()
    {
        // Arrange
        var editor = new RichTextEditorPlugin();
        var plugin = new AlignmentBidiPlugin(editor);
        var context = CreateContext();
        string? requestedCommand = null;
        object? requestedValue = null;
        editor.CommandRequested += (cmd, val) =>
        {
            requestedCommand = cmd;
            requestedValue = val;
        };

        // Act
        await plugin.ExecuteAsync(AlignmentBidiPlugin.AlignRightCommand, context, TestContext.Current.CancellationToken);
        Assert.Equal("justifyRight", requestedCommand);

        await plugin.ExecuteAsync(AlignmentBidiPlugin.DirectionRtlCommand, context, TestContext.Current.CancellationToken);
        Assert.Equal("dir", requestedCommand);
        Assert.Equal("rtl", requestedValue);
    }

    [Fact]
    public async Task TypographyPlugin_should_dispatch_typography_commands()
    {
        // Arrange
        var editor = new RichTextEditorPlugin();
        var plugin = new TypographyPlugin(editor);
        var context = CreateContext();
        string? requestedCommand = null;
        object? requestedValue = null;
        editor.CommandRequested += (cmd, val) =>
        {
            requestedCommand = cmd;
            requestedValue = val;
        };

        // Act
        await plugin.ExecuteAsync(TypographyPlugin.TextColorCommand, context, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("foreColor", requestedCommand);
        Assert.NotNull(requestedValue);
        Assert.Equal("typography", plugin.Id);
    }

    [Theory]
    [InlineData(ComposerFontSizeUnit.Rem, "16", "1rem")]
    [InlineData(ComposerFontSizeUnit.Rem, "12", "0.75rem")]
    [InlineData(ComposerFontSizeUnit.Rem, "72", "4.5rem")]
    [InlineData(ComposerFontSizeUnit.Points, "16", "16pt")]
    public void TypographyPlugin_should_write_a_font_size_in_the_configured_unit(
        ComposerFontSizeUnit unit,
        string label,
        string expected)
    {
        // Arrange
        var plugin = new TypographyPlugin(
            new RichTextEditorPlugin(),
            new TypographyPluginOptions { FontSizeUnit = unit, FontSizes = [label] });

        // Act
        var option = FindMenu(plugin.Items, "font-size").Options.Single();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(label, option.Title),
            () => Assert.Equal($"font-size:{expected}", option.Id),
            () => Assert.Equal($"{TypographyPlugin.FontSizeCommand}:{expected}", option.Command));
    }

    [Fact]
    public void TypographyPlugin_should_scale_authored_sizes_with_the_reader_by_default()
    {
        // Arrange
        var plugin = new TypographyPlugin(new RichTextEditorPlugin());

        // Act
        var options = FindMenu(plugin.Items, "font-size").Options;

        // Assert
        Assert.All(options, option => Assert.EndsWith("rem", option.Command, StringComparison.Ordinal));
    }

    private sealed class NullAssetLoader : IComposerAssetLoader
    {
        public ValueTask LoadAssetAsync(ComposerAsset asset, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask LoadAssetsAsync(IEnumerable<ComposerAsset> assets, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public bool IsAssetLoaded(ComposerAsset asset)
            => true;

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
