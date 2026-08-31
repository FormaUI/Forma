namespace Forma.Features.Composer.Tests.Editing;

using Forma.Features.Composer.Editing;
using Forma.Features.Composer.Plugins;

using Xunit;

public sealed class ComposerKeyMapTests
{
    private sealed class Source(params ComposerKeyBinding[] bindings) : IComposerKeyBindingSource
    {
        public IReadOnlyList<ComposerKeyBinding> KeyBindings { get; } = bindings;
    }

    private static ComposerKeyBinding Binding(string key, string command, bool shift = false)
        => new() { Key = key, Command = command, Control = true, Shift = shift };

    [Fact]
    public void Should_merge_bindings_from_every_provider()
    {
        // Arrange
        var first = new Source(Binding("b", "bold"));
        var second = new Source(Binding("i", "italic"));

        // Act
        var map = new ComposerKeyMap([first, second]);

        // Assert
        Assert.Equal(2, map.Bindings.Count);
    }

    [Fact]
    public void Should_find_the_binding_a_command_answers_to()
    {
        // Arrange
        var map = new ComposerKeyMap([new Source(Binding("b", "bold"))]);

        // Act
        var binding = map.Find("bold");

        // Assert
        Assert.Equal("Ctrl+B", binding?.Display);
    }

    [Fact]
    public void Should_let_the_earliest_provider_win_a_contested_command()
    {
        // Arrange
        var engine = new Source(Binding("z", "undo"));
        var plugin = new Source(Binding("u", "undo"));

        // Act
        var map = new ComposerKeyMap([engine, plugin]);

        // Assert
        Assert.Equal("Ctrl+Z", map.Find("undo")?.Display);
    }

    [Fact]
    public void Should_return_null_for_an_unbound_command()
    {
        // Arrange
        var map = new ComposerKeyMap([new Source(Binding("b", "bold"))]);

        // Act
        var binding = map.Find("italic");

        // Assert
        Assert.Null(binding);
    }

    [Fact]
    public void Should_bind_undo_and_redo_by_default()
    {
        // Arrange
        var map = new ComposerKeyMap([ComposerKeyBindingSource.Default]);

        // Act
        var undo = map.Find(ComposerCommands.Undo);

        // Assert
        Assert.Multiple(
            () => Assert.Equal("Ctrl+Z", undo?.Display),
            () => Assert.Equal("Control+Z", undo?.AriaKeyShortcuts),
            () => Assert.Equal("Ctrl+Y", map.Find(ComposerCommands.Redo)?.Display));
    }

    [Fact]
    public void Should_carry_every_default_chord_including_the_redo_alternative()
    {
        // Arrange
        var map = new ComposerKeyMap([ComposerKeyBindingSource.Default]);

        // Act
        var redoChords = map.Bindings.Where(b => b.Command == ComposerCommands.Redo).Select(b => b.Display);

        // Assert
        Assert.Equal(["Ctrl+Y", "Ctrl+Shift+Z"], redoChords);
    }

    [Fact]
    public void Should_contribute_nothing_when_empty()
    {
        // Act
        var map = ComposerKeyMap.Empty;

        // Assert
        Assert.Empty(map.Bindings);
    }
}
