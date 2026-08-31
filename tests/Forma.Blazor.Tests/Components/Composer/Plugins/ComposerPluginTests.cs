namespace Forma.Blazor.Tests.Components.Composer.Plugins;

using Forma.Blazor.Components.Composer.Plugins;
using Forma.Features.Composer.Documents;
using Forma.Features.Composer.Editing;
using Forma.Features.Composer.Plugins;
using Forma.Features.Sanitization;
using Forma.Tests.Composer;

public sealed class ComposerPluginTests
{
    private static readonly NullAssetLoader SharedAssetLoader = new();

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
            new AllowlistHtmlSanitizer(),
            SharedAssetLoader,
            regions,
            _ => Task.CompletedTask,
            () => Task.CompletedTask,
            _ => Task.CompletedTask,
            _ => Task.CompletedTask);
    }

    [Fact]
    public void MaximizePlugin_should_apply_to_every_editor()
    {
        // Arrange
        IComposerFeaturePlugin plugin = new MaximizePlugin();

        // Assert
        Assert.Equal("maximize", plugin.Id);
        Assert.Empty(plugin.SupportedEditorIds);
        Assert.NotEmpty(plugin.Items);
    }

    [Fact]
    public async Task MaximizePlugin_execute_should_be_a_no_op_because_the_engine_claims_the_command()
    {
        // Arrange
        var plugin = new MaximizePlugin();
        var context = CreateContext();

        // Act
        await plugin.ExecuteAsync(MaximizePlugin.MaximizeCommand, context, TestContext.Current.CancellationToken);

        // Assert (no exception, no state to observe: the engine owns the command)
    }

    [Fact]
    public void SelectionBarPlugin_should_apply_to_every_editor()
    {
        // Arrange
        IComposerFeaturePlugin plugin = new SelectionBarPlugin();

        // Assert
        Assert.Equal("selection-bar", plugin.Id);
        Assert.Empty(plugin.SupportedEditorIds);
        Assert.NotEmpty(plugin.Items);
    }

    [Fact]
    public async Task SelectionBarPlugin_execute_should_be_a_no_op_because_the_engine_claims_the_command()
    {
        // Arrange
        var plugin = new SelectionBarPlugin();
        var context = CreateContext();

        // Act
        await plugin.ExecuteAsync(SelectionBarPlugin.SelectionBarCommand, context, TestContext.Current.CancellationToken);

        // Assert (no exception, no state to observe: the engine owns the command)
    }
}
