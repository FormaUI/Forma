namespace Forma.Features.Explorer.Tests;

using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerSelectionTests
{
    private static ExplorerItem Item(string source, bool isDisabled = false)
    {
        var resource = new FileResourceInfo(source);

        return new ExplorerItem { Key = source, Name = resource.Name, IsDisabled = isDisabled, Resource = resource };
    }

    [Fact]
    public void Should_ignore_toggling_a_disabled_item()
    {
        // Arrange
        var selection = new ExplorerSelection();
        var item = Item("/media/photo.png", isDisabled: true);

        // Act
        selection.Toggle(item);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(0, selection.Count),
            () => Assert.False(selection.IsSelected(item)),
            () => Assert.Empty(selection.Items));
    }

    [Fact]
    public void Should_list_selected_folders_before_selected_items()
    {
        // Arrange
        var selection = new ExplorerSelection();
        var item = Item("/media/photo.png");
        var folder = ExplorerItem.ForFolder(new FolderResourceInfo("/photos"));

        // Act
        selection.Replace([item, folder]);

        // Assert
        Assert.Equal([folder, item], selection.Items);
    }

    [Fact]
    public void Should_keep_one_kind_selected_at_a_time()
    {
        // Arrange
        var selection = new ExplorerSelection();
        var item = Item("/media/photo.png");
        var folder = ExplorerItem.ForFolder(new FolderResourceInfo("/photos"));
        selection.Toggle(item);

        // Act
        selection.Toggle(folder);

        // Assert
        Assert.Multiple(
            () => Assert.True(selection.IsSelected(folder)),
            () => Assert.False(selection.IsSelected(item)));
    }
}
