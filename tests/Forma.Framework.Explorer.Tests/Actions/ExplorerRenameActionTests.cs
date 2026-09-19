namespace Forma.Framework.Explorer.Tests.Actions;

using Forma.Framework.Catalog;
using Forma.Framework.Explorer.Actions;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerRenameActionTests
{
    private static CatalogItem CreateItem(string source)
        => CatalogItem.For(new FileResourceInfo(source));

    [Fact]
    public void Should_open_with_the_given_target_and_starting_name()
    {
        // Arrange
        var rename = new ExplorerRenameAction<ICatalogItem>();

        // Act
        rename.Request(CreateItem("/media/photo.png"));

        // Assert
        Assert.Multiple(
            () => Assert.True(rename.IsActive),
            () => Assert.Equal("/media/photo.png", rename.Target?.Key),
            () => Assert.Equal("photo.png", rename.Name));
    }

    [Fact]
    public void Should_replace_the_previous_target_on_a_new_request()
    {
        // Arrange
        var rename = new ExplorerRenameAction<ICatalogItem>();
        rename.Request(CreateItem("/media/photo.png"));

        // Act
        rename.Request(CreateItem("/media/notes.md"));

        // Assert
        Assert.Equal("/media/notes.md", rename.Target?.Key);
    }

    [Fact]
    public void Should_reject_a_null_target()
        => Assert.Throws<ArgumentNullException>(() => new ExplorerRenameAction<ICatalogItem>().Request(null!));

    [Fact]
    public void Should_confirm_with_its_starting_name()
    {
        // Arrange
        var rename = new ExplorerRenameAction<ICatalogItem>();

        // Act
        rename.Request(CreateItem("/media/photo.png"));

        // Assert
        Assert.Multiple(
            () => Assert.True(rename.CanConfirm),
            () => Assert.Equal("photo.png", rename.NameToConfirm));
    }

    [Fact]
    public void Should_work_for_a_folder_target()
    {
        // Arrange
        var rename = new ExplorerRenameAction<ICatalogFolderItem>();

        // Act
        rename.Request(CatalogItem.ForFolder(new FolderResourceInfo("/photos")));

        // Assert
        Assert.Equal("/photos", rename.Target?.Key);
    }

    [Fact]
    public void Should_work_for_an_item_without_a_resource()
    {
        // Arrange
        var rename = new ExplorerRenameAction<ICatalogItem>();

        // Act
        rename.Request(new CatalogItem { Key = "ghost", Name = "Ghost" });

        // Assert
        Assert.Multiple(
            () => Assert.Equal("ghost", rename.Target?.Key),
            () => Assert.Equal("Ghost", rename.Name));
    }
}
