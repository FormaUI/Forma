namespace Forma.UI.Features.Composer.Tests.Editing;

using Forma.UI.Features.Composer.Editing;

using Xunit;

public sealed class ComposerSelectionTests
{
    [Fact]
    public void Should_report_a_caret_as_collapsed()
    {
        // Arrange
        var caret = new ComposerSelection { Start = 5, End = 5 };

        // Act
        var isCollapsed = caret.IsCollapsed;

        // Assert
        Assert.True(isCollapsed);
    }

    [Fact]
    public void Should_not_report_a_range_as_collapsed()
    {
        // Arrange
        var range = new ComposerSelection { Start = 2, End = 7 };

        // Act
        var isCollapsed = range.IsCollapsed;

        // Assert
        Assert.False(isCollapsed);
    }

    [Fact]
    public void Should_expose_an_empty_selection()
    {
        // Act
        var empty = ComposerSelection.Empty;

        // Assert
        Assert.Multiple(
            () => Assert.True(empty.IsCollapsed),
            () => Assert.Empty(empty.ActiveStyles),
            () => Assert.Null(empty.BlockType));
    }
}
