namespace Forma.Features.Composer.Tests.Editing;

using Forma.Features.Composer.Documents;
using Forma.Features.Composer.Editing;

using Xunit;

public sealed class ComposerHistoryTests
{
    private static ComposerDocument Doc(string content)
        => new(ComposerDocumentFormat.Html, content);

    [Fact]
    public void Should_not_allow_undo_when_only_the_initial_state_is_recorded()
    {
        // Arrange
        var history = new ComposerHistory();

        // Act
        history.Record(Doc("v1"));

        // Assert
        Assert.Multiple(
            () => Assert.False(history.CanUndo),
            () => Assert.False(history.CanRedo));
    }

    [Fact]
    public void Should_allow_undo_once_a_second_state_is_recorded()
    {
        // Arrange
        var history = new ComposerHistory();
        history.Record(Doc("v1"));

        // Act
        history.Record(Doc("v2"));

        // Assert
        Assert.True(history.CanUndo);
    }

    [Fact]
    public void Should_not_undo_when_nothing_was_recorded()
    {
        // Arrange
        var history = new ComposerHistory();

        // Act
        var undone = history.Undo();

        // Assert
        Assert.Null(undone);
    }

    [Fact]
    public void Should_return_the_preceding_state_on_undo()
    {
        // Arrange
        var history = new ComposerHistory();
        history.Record(Doc("v1"));
        history.Record(Doc("v2"));

        // Act
        var undone = history.Undo();

        // Assert
        Assert.Equal(Doc("v1"), undone);
    }

    [Fact]
    public void Should_step_back_one_state_per_undo()
    {
        // Arrange
        var history = new ComposerHistory();
        history.Record(Doc("v1"));
        history.Record(Doc("v2"));
        history.Record(Doc("v3"));

        // Act
        var first = history.Undo();
        var second = history.Undo();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(Doc("v2"), first),
            () => Assert.Equal(Doc("v1"), second),
            () => Assert.False(history.CanUndo));
    }

    [Fact]
    public void Should_allow_redo_after_an_undo()
    {
        // Arrange
        var history = new ComposerHistory();
        history.Record(Doc("v1"));
        history.Record(Doc("v2"));

        // Act
        history.Undo();

        // Assert
        Assert.True(history.CanRedo);
    }

    [Fact]
    public void Should_not_redo_when_nothing_was_undone()
    {
        // Arrange
        var history = new ComposerHistory();
        history.Record(Doc("v1"));

        // Act
        var redone = history.Redo();

        // Assert
        Assert.Null(redone);
    }

    [Fact]
    public void Should_return_the_state_it_was_undone_from_on_redo()
    {
        // Arrange
        var history = new ComposerHistory();
        history.Record(Doc("v1"));
        history.Record(Doc("v2"));
        history.Undo();

        // Act
        var redone = history.Redo();

        // Assert
        Assert.Equal(Doc("v2"), redone);
    }

    [Fact]
    public void Should_clear_the_redo_stack_when_a_new_state_is_recorded()
    {
        // Arrange
        var history = new ComposerHistory();
        history.Record(Doc("v1"));
        history.Record(Doc("v2"));
        history.Undo();

        // Act
        history.Record(Doc("v3"));

        // Assert
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Should_clear_the_redo_stack_when_recording_a_state_matching_the_top_of_the_stack()
    {
        // Arrange
        var history = new ComposerHistory();
        history.Record(Doc("v1"));
        history.Record(Doc("v2"));
        history.Undo();

        // Act
        history.Record(Doc("v1"));

        // Assert
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Should_discard_the_oldest_state_beyond_capacity()
    {
        // Arrange
        var history = new ComposerHistory(maxCapacity: 2);
        history.Record(Doc("v1"));
        history.Record(Doc("v2"));
        history.Record(Doc("v3"));

        // Act
        var undone = history.Undo();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(Doc("v2"), undone),
            () => Assert.False(history.CanUndo));
    }

    [Fact]
    public void Should_reject_a_capacity_below_one()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ComposerHistory(0));
    }

    [Fact]
    public void Should_clear_history()
    {
        // Arrange
        var history = new ComposerHistory();
        history.Record(Doc("v1"));
        history.Record(Doc("v2"));
        history.Undo();

        // Act
        history.Clear();

        // Assert
        Assert.Multiple(
            () => Assert.False(history.CanUndo),
            () => Assert.False(history.CanRedo));
    }
}
