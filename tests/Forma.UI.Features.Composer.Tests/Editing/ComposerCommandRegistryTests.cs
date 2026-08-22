namespace Forma.UI.Features.Composer.Tests.Editing;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Documents;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Composer.Sanitization;

using Xunit;

public sealed class ComposerCommandRegistryTests
{
    private sealed class NullAssetLoader : IComposerAssetLoader
    {
        public bool IsAssetLoaded(ComposerAsset asset) => false;
        public ValueTask LoadAssetAsync(ComposerAsset asset, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask LoadAssetsAsync(IEnumerable<ComposerAsset> assets, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class RecordingPlugin(string id, params string[] commands) : IComposerPlugin
    {
        public string Id => id;
        public List<string> Executed { get; } = [];
        public IReadOnlyList<ComposerToolbarItem> ToolbarItems { get; } =
            [.. commands.Select(c => new ComposerToolbarItem { Id = c, Title = c, Command = c })];
        public IReadOnlyList<ComposerKeyBinding> KeyBindings => [];
        public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];
        public IReadOnlyList<ComposerAsset> Assets => [];
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);

        public ValueTask ExecuteAsync(string command, ComposerContext context, CancellationToken cancellationToken = default)
        {
            Executed.Add(command);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class OptionPlugin(string id, string itemId, params string[] optionCommands) : IComposerPlugin
    {
        public string Id => id;
        public List<string> Executed { get; } = [];
        public IReadOnlyList<ComposerToolbarItem> ToolbarItems { get; } =
        [
            new()
            {
                Id = itemId,
                Title = itemId,
                Options = [.. optionCommands.Select(c => new ComposerToolbarOption { Id = c, Title = c, Command = c })],
            },
        ];
        public IReadOnlyList<ComposerKeyBinding> KeyBindings => [];
        public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];
        public IReadOnlyList<ComposerAsset> Assets => [];
        public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);

        public ValueTask ExecuteAsync(string command, ComposerContext context, CancellationToken cancellationToken = default)
        {
            Executed.Add(command);
            return ValueTask.CompletedTask;
        }
    }

    private static readonly NullAssetLoader AssetLoader = new();

    private static ComposerContext CreateContext()
        => new(
            "editor",
            new ComposerDocument(ComposerDocumentFormat.PlainText, "text"),
            ComposerSelection.Empty,
            new ComposerHistory(),
            new DefaultHtmlSanitizer(),
            AssetLoader,
            new ComposerRegionRegistry(),
            _ => Task.CompletedTask,
            () => Task.CompletedTask,
            _ => Task.CompletedTask);

    private static Task<ComposerCommandOutcome> ExecuteAsync(ComposerCommandRegistry registry, string command)
        => registry.ExecuteAsync(command, CreateContext(), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Should_claim_a_command_a_toolbar_item_names_only_through_an_option()
    {
        // Arrange
        var registry = new ComposerCommandRegistry();
        var plugin = new OptionPlugin("typography", "text-color", "typography.text-color:#d32f2f");
        registry.RegisterPlugins([plugin]);

        // Act
        var outcome = await ExecuteAsync(registry, "typography.text-color:#d32f2f");

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ComposerCommandOutcome.Plugin, outcome),
            () => Assert.Equal(["typography.text-color:#d32f2f"], plugin.Executed));
    }

    [Fact]
    public async Task Should_run_an_engine_command()
    {        // Arrange
        var registry = new ComposerCommandRegistry();
        var ran = false;
        registry.RegisterEngineCommand("engine.go", () => { ran = true; return Task.CompletedTask; });

        // Act
        var outcome = await ExecuteAsync(registry, "engine.go");

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ComposerCommandOutcome.Engine, outcome),
            () => Assert.True(ran));
    }

    [Fact]
    public async Task Should_route_a_command_to_the_plugin_that_named_it()
    {
        // Arrange
        var plugin = new RecordingPlugin("p", "plugin.go");
        var registry = new ComposerCommandRegistry();
        registry.RegisterPlugins([plugin]);

        // Act
        var outcome = await ExecuteAsync(registry, "plugin.go");

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ComposerCommandOutcome.Plugin, outcome),
            () => Assert.Equal(["plugin.go"], plugin.Executed));
    }

    [Fact]
    public async Task Should_report_an_unknown_command_as_unhandled()
    {
        // Arrange
        var registry = new ComposerCommandRegistry();

        // Act
        var outcome = await ExecuteAsync(registry, "nobody.owns.this");

        // Assert
        Assert.Equal(ComposerCommandOutcome.Unhandled, outcome);
    }

    [Fact]
    public async Task Should_let_an_engine_command_win_over_a_plugin_claiming_the_same_name()
    {
        // Arrange
        var plugin = new RecordingPlugin("p", ComposerCommands.Undo);
        var registry = new ComposerCommandRegistry();
        registry.RegisterPlugins([plugin]);
        registry.RegisterEngineCommand(ComposerCommands.Undo, () => Task.CompletedTask);

        // Act
        var outcome = await ExecuteAsync(registry, ComposerCommands.Undo);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ComposerCommandOutcome.Engine, outcome),
            () => Assert.Empty(plugin.Executed));
    }

    [Fact]
    public void Should_replace_plugin_commands_when_the_plugin_set_changes()
    {
        // Arrange
        var registry = new ComposerCommandRegistry();
        registry.RegisterPlugins([new RecordingPlugin("old", "old.go")]);

        // Act
        registry.RegisterPlugins([new RecordingPlugin("new", "new.go")]);

        // Assert
        Assert.Equal(["new.go"], registry.Commands);
    }
}
