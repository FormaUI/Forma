namespace Forma.Features.Explorer.Tests;

using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerCreateFolderActionTests
{
    [Fact]
    public void Should_open_fresh_under_the_given_parent()
    {
        // Arrange
        var create = new ExplorerCreateFolderAction();

        // Act
        var parent = new FolderResourceInfo("/photos");
        create.Request(parent);

        // Assert
        Assert.Multiple(
            () => Assert.True(create.IsActive),
            () => Assert.Same(parent, create.Parent),
            () => Assert.Equal(string.Empty, create.Name));
    }

    [Fact]
    public void Should_open_at_the_top_level_with_no_parent()
    {
        // Arrange
        var create = new ExplorerCreateFolderAction();

        // Act
        create.Request(null);

        // Assert
        Assert.Multiple(
            () => Assert.True(create.IsActive),
            () => Assert.Null(create.Parent));
    }

    [Fact]
    public void Should_not_confirm_without_a_name()
    {
        // Arrange
        var create = new ExplorerCreateFolderAction();

        // Act
        create.Request(null);

        // Assert
        Assert.False(create.CanConfirm);
    }
}
