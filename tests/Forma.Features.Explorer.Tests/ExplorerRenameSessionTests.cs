namespace Forma.Features.Explorer.Tests;

using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerRenameSessionTests
{
    [Fact]
    public void Should_hold_one_rename_at_a_time()
    {
        // Arrange
        var rename = new ExplorerRenameSession();
        rename.BeginRenaming(new FileResourceInfo("/media/photo.png"));

        // Act
        rename.BeginRenaming(new FolderResourceInfo("/photos"));

        // Assert
        Assert.Multiple(
            () => Assert.Null(rename.Resource),
            () => Assert.Equal("/photos", rename.Folder?.Source),
            () => Assert.Equal("photos", rename.DraftName));
    }

    [Fact]
    public void Should_tell_a_folder_target_from_a_resource_target()
    {
        // Arrange
        var rename = new ExplorerRenameSession();

        // Act
        rename.BeginRenaming(new FileResourceInfo("/media/photo.png"));

        // Assert
        Assert.Multiple(
            () => Assert.Null(rename.Folder),
            () => Assert.Equal("photo.png", rename.Resource?.Name));
    }

    [Fact]
    public void Should_confirm_only_a_usable_name()
    {
        // Arrange
        var rename = new ExplorerRenameSession();
        rename.BeginRenaming(new FileResourceInfo("/media/photo.png"));

        // Act & Assert
        rename.DraftName = "   ";
        Assert.False(rename.CanConfirm);

        rename.DraftName = "  stage-one.png  ";
        Assert.Multiple(
            () => Assert.True(rename.CanConfirm),
            () => Assert.Equal("stage-one.png", rename.ConfirmedName));
    }

    [Fact]
    public void Should_hold_nothing_once_closed()
    {
        // Arrange
        var rename = new ExplorerRenameSession();
        rename.BeginRenaming(new FileResourceInfo("/media/photo.png"));

        // Act
        rename.EndRenaming();

        // Assert
        Assert.Multiple(
            () => Assert.Null(rename.Target),
            () => Assert.Equal(string.Empty, rename.DraftName),
            () => Assert.False(rename.CanConfirm));
    }
}
