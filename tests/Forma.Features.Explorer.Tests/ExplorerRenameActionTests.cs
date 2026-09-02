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
    public void Should_confirm_with_its_starting_name()
    {
        // Arrange
        var rename = new ExplorerRenameAction<ResourceInfo>();

        // Act
        rename.Request(new FileResourceInfo("/media/photo.png"));

        // Assert
        Assert.Multiple(
            () => Assert.True(rename.CanConfirm),
            () => Assert.Equal("photo.png", rename.NameToConfirm));
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
