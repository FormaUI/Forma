namespace Forma.Features.Explorer.Tests;

using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerRenameActionTests
{
    [Fact]
    public void Should_open_with_the_given_target_and_starting_name()
    {
        // Arrange
        var rename = new ExplorerRenameAction<ResourceInfo>();

        // Act
        rename.Request(new FileResourceInfo("/media/photo.png"));

        // Assert
        Assert.Multiple(
            () => Assert.True(rename.IsActive),
            () => Assert.Equal("/media/photo.png", rename.Target?.Source),
            () => Assert.Equal("photo.png", rename.Name));
    }

    [Fact]
    public void Should_replace_the_previous_target_on_a_new_request()
    {
        // Arrange
        var rename = new ExplorerRenameAction<ResourceInfo>();
        rename.Request(new FileResourceInfo("/media/photo.png"));

        // Act
        rename.Request(new FileResourceInfo("/media/notes.md"));

        // Assert
        Assert.Equal("/media/notes.md", rename.Target?.Source);
    }

    [Fact]
    public void Should_reject_a_null_target()
        => Assert.Throws<ArgumentNullException>(() => new ExplorerRenameAction<ResourceInfo>().Request(null!));

    [Fact]
    public void Should_confirm_only_a_usable_name()
    {
        // Arrange
        var rename = new ExplorerRenameAction<ResourceInfo>();
        rename.Request(new FileResourceInfo("/media/photo.png"));

        // Act & Assert
        rename.Name = "   ";
        Assert.False(rename.CanConfirm);

        rename.Name = "  stage-one.png  ";
        Assert.Multiple(
            () => Assert.True(rename.CanConfirm),
            () => Assert.Equal("stage-one.png", rename.NameToConfirm));
    }

    [Fact]
    public void Should_hold_nothing_once_cleared()
    {
        // Arrange
        var rename = new ExplorerRenameAction<ResourceInfo>();
        rename.Request(new FileResourceInfo("/media/photo.png"));

        // Act
        rename.Clear();

        // Assert
        Assert.Multiple(
            () => Assert.False(rename.IsActive),
            () => Assert.Null(rename.Target),
            () => Assert.Equal(string.Empty, rename.Name),
            () => Assert.False(rename.CanConfirm));
    }

    [Fact]
    public void Should_work_for_a_folder_target()
    {
        // Arrange
        var rename = new ExplorerRenameAction<FolderResourceInfo>();

        // Act
        rename.Request(new FolderResourceInfo("/photos"));

        // Assert
        Assert.Equal("/photos", rename.Target?.Source);
    }
}
