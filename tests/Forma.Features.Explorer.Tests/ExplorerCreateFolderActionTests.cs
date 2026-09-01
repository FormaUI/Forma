namespace Forma.Features.Explorer.Tests;

using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerCreateFolderActionTests
{
    [Fact]
    public void Should_open_fresh_under_the_given_parent()
    {
        // Arrange
        var create = new ExplorerCreateFolderAction { Name = "left over" };

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
    public void Should_create_only_with_a_usable_name()
    {
        // Arrange
        var create = new ExplorerCreateFolderAction();
        create.Request(null);

        // Act & Assert
        Assert.False(create.CanConfirm);

        create.Name = "  Races  ";
        Assert.Multiple(
            () => Assert.True(create.CanConfirm),
            () => Assert.Equal("Races", create.NameToConfirm));
    }

    [Fact]
    public void Should_hold_nothing_once_cleared()
    {
        // Arrange
        var create = new ExplorerCreateFolderAction();
        create.Request(new FolderResourceInfo("/photos"));
        create.Name = "Races";

        // Act
        create.Clear();

        // Assert
        Assert.Multiple(
            () => Assert.False(create.IsActive),
            () => Assert.Null(create.Parent),
            () => Assert.Equal(string.Empty, create.Name),
            () => Assert.False(create.CanConfirm));
    }
}
