namespace Forma.Features.Explorer.Tests;

using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerDeletionTests
{
    [Fact]
    public void Should_hold_one_pending_deletion_at_a_time()
    {
        // Arrange
        var deletion = new ExplorerDeletion();
        deletion.Request([new FileResourceInfo("/media/photo.png")]);

        // Act
        deletion.Request([new FolderResourceInfo("/photos")]);

        // Assert
        Assert.Multiple(
            () => Assert.Empty(deletion.Resources),
            () => Assert.Single(deletion.Folders));
    }

    [Fact]
    public void Should_hold_nothing_once_cleared()
    {
        // Arrange
        var deletion = new ExplorerDeletion();
        deletion.Request([new FileResourceInfo("/media/photo.png")]);

        // Act
        deletion.Clear();

        // Assert
        Assert.Multiple(
            () => Assert.Empty(deletion.Resources),
            () => Assert.Empty(deletion.Folders));
    }

    [Fact]
    public void Should_keep_its_own_copy_of_the_requested_resources()
    {
        // Arrange
        var deletion = new ExplorerDeletion();
        var requested = new List<ResourceInfo> { new FileResourceInfo("/media/photo.png") };

        // Act
        deletion.Request(requested);
        requested.Clear();

        // Assert
        Assert.Single(deletion.Resources);
    }
}
