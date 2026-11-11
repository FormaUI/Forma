namespace Forma.Framework.Catalog.Tests.Selection;

using Forma.Framework.Catalog.Selection;
using Forma.Framework.Resources;

using Xunit;

public sealed class CatalogSelectionTests
{
    private static CatalogItem CreateItem(string source, bool isDisabled = false)
    {
        var resource = new FileResourceInfo(source);

        return new CatalogItem { Key = source, Name = resource.Name, IsDisabled = isDisabled, Resource = resource };
    }

    private static CatalogFolderItem CreateFolderItem(string source)
        => CatalogItem.ForFolder(new FolderResourceInfo(source));

    [Fact]
    public void Should_ignore_toggling_a_disabled_item()
    {
        // Arrange
        var selection = new CatalogSelection();
        var item = CreateItem("/media/photo.png", isDisabled: true);

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
        var selection = new CatalogSelection();
        var item = CreateItem("/media/photo.png");
        var folder = CreateFolderItem("/photos");

        // Act
        selection.Replace([item, folder]);

        // Assert
        Assert.Equal([folder, item], selection.Items);
    }

    [Fact]
    public void Should_keep_a_folder_and_a_file_selected_together()
    {
        // Arrange
        var selection = new CatalogSelection();
        var item = CreateItem("/media/photo.png");
        var folder = CreateFolderItem("/photos");
        selection.Toggle(item);

        // Act
        selection.Toggle(folder);

        // Assert
        Assert.Multiple(
            () => Assert.True(selection.IsSelected(folder)),
            () => Assert.True(selection.IsSelected(item)),
            () => Assert.Equal(2, selection.Count),
            () => Assert.Equal([folder, item], selection.Items));
    }

    [Fact]
    public void Should_count_folders_and_files_together_toward_the_selection_limit()
    {
        // Arrange
        var selection = new CatalogSelection { SelectionLimit = 2 };
        var item = CreateItem("/media/photo.png");
        var folder = CreateFolderItem("/photos");
        var otherItem = CreateItem("/media/cover.png");
        var otherFolder = CreateFolderItem("/covers");
        selection.Toggle(item);
        selection.Toggle(folder);

        // Act
        selection.Toggle(otherItem);
        selection.Toggle(otherFolder);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(2, selection.Count),
            () => Assert.Equal([folder, item], selection.Items));
    }

    [Fact]
    public void Should_replace_either_kind_when_the_selection_limit_is_one()
    {
        // Arrange
        var selection = new CatalogSelection { SelectionLimit = 1 };
        var item = CreateItem("/media/photo.png");
        var folder = CreateFolderItem("/photos");
        var otherItem = CreateItem("/media/cover.png");
        selection.Toggle(item);

        // Act
        selection.Toggle(folder);
        var selectedAfterFolder = selection.Items;
        selection.Toggle(otherItem);

        // Assert
        Assert.Multiple(
            () => Assert.Equal([folder], selectedAfterFolder),
            () => Assert.Equal([otherItem], selection.Items));
    }
}
