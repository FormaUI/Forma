namespace Forma.Features.Explorer.Tests;

using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerRenameActionTests
{
    private static ExplorerItem Item(string source)
        => ExplorerItem.For(new FileResourceInfo(source));

    [Fact]
    public void Should_open_with_the_given_target_and_starting_name()
    {
        // Arrange
        var rename = new ExplorerRenameAction<IExplorerItem>();

        // Act
        rename.Request(Item("/media/photo.png"));

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
        var rename = new ExplorerRenameAction<IExplorerItem>();
        rename.Request(Item("/media/photo.png"));

        // Act
        rename.Request(Item("/media/notes.md"));

        // Assert
        Assert.Equal("/media/notes.md", rename.Target?.Key);
    }

    [Fact]
    public void Should_reject_a_null_target()
        => Assert.Throws<ArgumentNullException>(() => new ExplorerRenameAction<IExplorerItem>().Request(null!));

    [Fact]
    public void Should_confirm_with_its_starting_name()
    {
        // Arrange
        var rename = new ExplorerRenameAction<IExplorerItem>();

        // Act
        rename.Request(Item("/media/photo.png"));

        // Assert
        Assert.Multiple(
            () => Assert.True(rename.CanConfirm),
            () => Assert.Equal("photo.png", rename.NameToConfirm));
    }

    [Fact]
    public void Should_work_for_a_folder_target()
    {
        // Arrange
        var rename = new ExplorerRenameAction<IExplorerFolderItem>();

        // Act
        rename.Request(ExplorerItem.ForFolder(new FolderResourceInfo("/photos")));

        // Assert
        Assert.Equal("/photos", rename.Target?.Key);
    }

    [Fact]
    public void Should_work_for_an_item_without_a_resource()
    {
        // Arrange
        var rename = new ExplorerRenameAction<IExplorerItem>();

        // Act
        rename.Request(new ExplorerItem { Key = "ghost", Name = "Ghost" });

        // Assert
        Assert.Multiple(
            () => Assert.Equal("ghost", rename.Target?.Key),
            () => Assert.Equal("Ghost", rename.Name));
    }
}
