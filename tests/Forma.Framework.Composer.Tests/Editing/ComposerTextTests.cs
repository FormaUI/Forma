namespace Forma.Framework.Composer.Tests.Editing;

using Forma.Framework.Composer.Editing;

using Xunit;

public sealed class ComposerTextTests
{
    [Fact]
    public void Should_return_the_word_touching_a_collapsed_caret()
    {
        // Arrange
        const string content = "hello world";

        // Act
        var word = ComposerText.GetWordAt(content, 2);

        // Assert
        Assert.Equal("hello", word.Text);
    }

    [Fact]
    public void Should_return_an_empty_span_when_neither_neighbour_is_a_word_character()
    {
        // Arrange
        const string content = "a  b";

        // Act
        var word = ComposerText.GetWordAt(content, 2);

        // Assert
        Assert.Equal(string.Empty, word.Text);
    }

    [Fact]
    public void Should_treat_underscore_as_a_word_character()
    {
        // Arrange
        const string content = "my_variable here";

        // Act
        var word = ComposerText.GetWordAt(content, 3);

        // Assert
        Assert.Equal("my_variable", word.Text);
    }

    [Fact]
    public void Should_return_the_text_a_selection_spans_regardless_of_anchor_direction()
    {
        // Arrange
        const string content = "hello world";

        // Act
        var forward = ComposerText.GetSelected(content, new ComposerSelection { Start = 0, End = 5 });
        var backward = ComposerText.GetSelected(content, new ComposerSelection { Start = 5, End = 0 });

        // Assert
        Assert.Multiple(
            () => Assert.Equal("hello", forward.Text),
            () => Assert.Equal("hello", backward.Text));
    }

    [Fact]
    public void Should_return_the_single_line_a_collapsed_caret_sits_on()
    {
        // Arrange
        const string content = "one\ntwo\nthree";

        // Act
        var lines = ComposerText.GetLines(content, new ComposerSelection { Start = 5, End = 5 });

        // Assert
        var line = Assert.Single(lines);
        Assert.Equal("two", line.Text);
    }

    [Fact]
    public void Should_return_every_line_a_multiline_selection_touches()
    {
        // Arrange
        const string content = "one\ntwo\nthree";

        // Act
        var lines = ComposerText.GetLines(content, new ComposerSelection { Start = 1, End = 9 });

        // Assert
        Assert.Equal(["one", "two", "three"], lines.Select(l => l.Text));
    }

    [Fact]
    public void Should_replace_the_given_range_and_place_the_caret_after_it()
    {
        // Arrange
        const string content = "hello world";

        // Act
        var edit = ComposerText.Replace(content, 0, 5, "goodbye");

        // Assert
        Assert.Multiple(
            () => Assert.Equal("goodbye world", edit.Content),
            () => Assert.Equal(7, edit.Selection.Start),
            () => Assert.Equal(7, edit.Selection.End));
    }

    [Fact]
    public void Should_replace_a_range_given_in_reverse_order()
    {
        // Arrange
        const string content = "hello world";

        // Act
        var edit = ComposerText.Replace(content, 5, 0, "goodbye");

        // Assert
        Assert.Equal("goodbye world", edit.Content);
    }

    [Fact]
    public void Should_replace_every_occurrence_of_a_value()
    {
        // Arrange
        const string content = "cat cat dog";

        // Act
        var edit = ComposerText.ReplaceAll(content, "cat", "bird");

        // Assert
        Assert.Equal("bird bird dog", edit.Content);
    }

    [Fact]
    public void Should_leave_the_caret_at_the_end_of_the_document_after_replacing_all()
    {
        // Arrange
        const string content = "cat cat";

        // Act
        var edit = ComposerText.ReplaceAll(content, "cat", "bird");

        // Assert
        Assert.Multiple(
            () => Assert.Equal(edit.Content.Length, edit.Selection.Start),
            () => Assert.Equal(edit.Content.Length, edit.Selection.End));
    }

    [Fact]
    public void Should_ignore_case_when_asked_to()
    {
        // Arrange
        const string content = "Cat cat CAT";

        // Act
        var edit = ComposerText.ReplaceAll(content, "cat", "dog", StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.Equal("dog dog dog", edit.Content);
    }

    [Fact]
    public void Should_clamp_an_out_of_range_offset_rather_than_throw()
    {
        // Arrange
        const string content = "hello";

        // Act
        var word = ComposerText.GetWordAt(content, 999);

        // Assert
        Assert.Equal("hello", word.Text);
    }
}
