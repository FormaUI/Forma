namespace Forma.Framework.Explorer.Tests.Actions;

using Forma.Framework.Catalog;
using Forma.Framework.Explorer.Actions;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerBatchActionTests
{
    private static CatalogItem CreateItem(string source)
        => CatalogItem.For(new FileResourceInfo(source));

    [Fact]
    public void Should_hold_the_requested_targets()
    {
        // Arrange
        var batch = new ExplorerBatchAction<ICatalogItem>();

        // Act
        batch.Request([CreateItem("/media/photo.png")]);

        // Assert
        Assert.Multiple(
            () => Assert.True(batch.IsActive),
            () => Assert.Single(batch.Targets));
    }

    [Fact]
    public void Should_replace_the_previous_request_on_a_new_one()
    {
        // Arrange
        var batch = new ExplorerBatchAction<ICatalogItem>();
        batch.Request([CreateItem("/media/photo.png")]);

        // Act
        batch.Request([CreateItem("/media/a.png"), CreateItem("/media/b.png")]);

        // Assert
        Assert.Equal(2, batch.Targets.Count);
    }

    [Fact]
    public void Should_reject_an_empty_request()
        => Assert.Throws<ArgumentException>(() => new ExplorerBatchAction<ICatalogItem>().Request([]));

    [Fact]
    public void Should_keep_its_own_copy_of_the_requested_targets()
    {
        // Arrange
        var batch = new ExplorerBatchAction<ICatalogItem>();
        var requested = new List<ICatalogItem> { CreateItem("/media/photo.png") };

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
        var batch = new ExplorerBatchAction<ICatalogFolderItem>();

        // Act
        batch.Request([CatalogItem.ForFolder(new FolderResourceInfo("/photos"))]);

        // Assert
        Assert.Single(batch.Targets);
    }
}
