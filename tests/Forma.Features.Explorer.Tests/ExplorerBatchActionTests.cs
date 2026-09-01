namespace Forma.Features.Explorer.Tests;

using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerBatchActionTests
{
    [Fact]
    public void Should_hold_the_requested_targets()
    {
        // Arrange
        var batch = new ExplorerBatchAction<ResourceInfo>();

        // Act
        batch.Request([new FileResourceInfo("/media/photo.png")]);

        // Assert
        Assert.Multiple(
            () => Assert.True(batch.IsActive),
            () => Assert.Single(batch.Targets));
    }

    [Fact]
    public void Should_replace_the_previous_request_on_a_new_one()
    {
        // Arrange
        var batch = new ExplorerBatchAction<ResourceInfo>();
        batch.Request([new FileResourceInfo("/media/photo.png")]);

        // Act
        batch.Request([new FileResourceInfo("/media/a.png"), new FileResourceInfo("/media/b.png")]);

        // Assert
        Assert.Equal(2, batch.Targets.Count);
    }

    [Fact]
    public void Should_reject_an_empty_request()
        => Assert.Throws<ArgumentException>(() => new ExplorerBatchAction<ResourceInfo>().Request([]));

    [Fact]
    public void Should_hold_nothing_once_cleared()
    {
        // Arrange
        var batch = new ExplorerBatchAction<ResourceInfo>();
        batch.Request([new FileResourceInfo("/media/photo.png")]);

        // Act
        batch.Clear();

        // Assert
        Assert.Multiple(
            () => Assert.False(batch.IsActive),
            () => Assert.Empty(batch.Targets));
    }

    [Fact]
    public void Should_keep_its_own_copy_of_the_requested_targets()
    {
        // Arrange
        var batch = new ExplorerBatchAction<ResourceInfo>();
        var requested = new List<ResourceInfo> { new FileResourceInfo("/media/photo.png") };

        // Act
        batch.Request(requested);
        requested.Clear();

        // Assert
        Assert.Single(batch.Targets);
    }

    [Fact]
    public void Should_work_for_folder_targets()
    {
        // Arrange
        var batch = new ExplorerBatchAction<FolderResourceInfo>();

        // Act
        batch.Request([new FolderResourceInfo("/photos")]);

        // Assert
        Assert.Single(batch.Targets);
    }
}
