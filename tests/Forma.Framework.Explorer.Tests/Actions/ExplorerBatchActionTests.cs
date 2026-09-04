namespace Forma.Framework.Explorer.Tests.Actions;

using Forma.Framework.Explorer.Actions;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerBatchActionTests
{
    private static ExplorerItem Item(string source)
        => ExplorerItem.For(new FileResourceInfo(source));

    [Fact]
    public void Should_hold_the_requested_targets()
    {
        // Arrange
        var batch = new ExplorerBatchAction<IExplorerItem>();

        // Act
        batch.Request([Item("/media/photo.png")]);

        // Assert
        Assert.Multiple(
            () => Assert.True(batch.IsActive),
            () => Assert.Single(batch.Targets));
    }

    [Fact]
    public void Should_replace_the_previous_request_on_a_new_one()
    {
        // Arrange
        var batch = new ExplorerBatchAction<IExplorerItem>();
        batch.Request([Item("/media/photo.png")]);

        // Act
        batch.Request([Item("/media/a.png"), Item("/media/b.png")]);

        // Assert
        Assert.Equal(2, batch.Targets.Count);
    }

    [Fact]
    public void Should_reject_an_empty_request()
        => Assert.Throws<ArgumentException>(() => new ExplorerBatchAction<IExplorerItem>().Request([]));

    [Fact]
    public void Should_keep_its_own_copy_of_the_requested_targets()
    {
        // Arrange
        var batch = new ExplorerBatchAction<IExplorerItem>();
        var requested = new List<IExplorerItem> { Item("/media/photo.png") };

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
        var batch = new ExplorerBatchAction<IExplorerFolderItem>();

        // Act
        batch.Request([ExplorerItem.ForFolder(new FolderResourceInfo("/photos"))]);

        // Assert
        Assert.Single(batch.Targets);
    }
}
