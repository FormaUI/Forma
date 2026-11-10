namespace Forma.Framework.Catalog.Tests.Navigation;

using Forma.Framework.Catalog.Navigation;
using Forma.Framework.Resources;

using Xunit;

public sealed class CatalogFolderTreeTests
{
    private static readonly ICatalogItem Photos = CatalogItem.For(new FolderResourceInfo("/photos"));
    private static readonly ICatalogItem Races = CatalogItem.For(new FolderResourceInfo("/photos/races") { Parent = "/photos" });
    private static readonly ICatalogItem Stages = CatalogItem.For(new FolderResourceInfo("/photos/races/stages") { Parent = "/photos/races" });
    private static readonly ICatalogItem Documents = CatalogItem.For(new FolderResourceInfo("/documents"));

    [Fact]
    public void Should_hold_nothing_when_given_no_folders()
    {
        // Assert
        Assert.Multiple(
            () => Assert.Empty(CatalogFolderTree.Empty.GetChildren(null)),
            () => Assert.Empty(new CatalogFolderTree(null).GetChildren(null)));
    }

    [Fact]
    public void Should_treat_a_folder_naming_no_parent_as_top_level()
    {
        // Act
        var tree = new CatalogFolderTree([Photos, Races, Documents]);

        // Assert
        Assert.Equal([Photos, Documents], tree.GetChildren(null));
    }

    [Fact]
    public void Should_find_the_folders_directly_inside_a_parent()
    {
        // Act
        var tree = new CatalogFolderTree([Photos, Races, Stages]);

        // Assert
        Assert.Multiple(
            () => Assert.Equal([Races], tree.GetChildren("/photos")),
            () => Assert.Equal([Stages], tree.GetChildren("/photos/races")),
            () => Assert.Empty(tree.GetChildren("/photos/races/stages")));
    }

    [Fact]
    public void Should_hold_nothing_for_a_parent_the_tree_does_not_know()
    {
        // Act
        var tree = new CatalogFolderTree([Photos]);

        // Assert
        Assert.Empty(tree.GetChildren("/nowhere"));
    }

    [Fact]
    public void Should_find_a_folder_by_its_source()
    {
        // Act
        var tree = new CatalogFolderTree([Photos, Races]);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(Races, tree.Find("/photos/races")),
            () => Assert.Null(tree.Find("/nowhere")),
            () => Assert.Null(tree.Find(null)));
    }

    [Fact]
    public void Should_let_the_first_folder_claiming_a_source_win()
    {
        // Arrange
        var repeated = CatalogItem.For(new FolderResourceInfo("/photos") { Name = "second" });

        // Act
        var tree = new CatalogFolderTree([Photos, repeated]);

        // Assert — the first is the folder by that key, and the second stands nowhere in the tree
        Assert.Multiple(
            () => Assert.Equal(Photos, tree.Find("/photos")),
            () => Assert.Equal([Photos], tree.GetChildren(null)),
            () => Assert.Equal([Photos], tree.GetListedChildren(null)));
    }

    [Fact]
    public void Should_tell_a_folder_holding_others_from_an_empty_one()
    {
        // Act
        var tree = new CatalogFolderTree([Photos, Races]);

        // Assert
        Assert.Multiple(
            () => Assert.True(tree.HasChildren("/photos")),
            () => Assert.False(tree.HasChildren("/photos/races")));
    }

    [Fact]
    public void Should_build_the_hierarchy_from_item_parents_without_resources()
    {
        // Arrange
        var top = new CatalogFolderItem { Key = "top", Name = "Top" };
        var nested = new CatalogFolderItem { Key = "top/nested", Name = "Nested", ParentKey = "top" };

        // Act
        var tree = new CatalogFolderTree([top, nested]);

        // Assert
        Assert.Multiple(
            () => Assert.Equal([top], tree.GetChildren(null)),
            () => Assert.Equal([nested], tree.GetChildren("top")),
            () => Assert.Same(nested, tree.Find("top/nested")));
    }

    [Fact]
    public void Should_list_only_the_folders_that_are_not_hidden()
    {
        // Arrange
        var shown = new CatalogFolderItem { Key = "shown", Name = "Shown" };
        var hidden = new CatalogFolderItem { Key = "hidden", Name = "Hidden", Visibility = CatalogItemVisibility.Hidden };
        var nestedHidden = new CatalogFolderItem { Key = "shown/hidden", Name = "Nested", Visibility = CatalogItemVisibility.Hidden, ParentKey = "shown" };

        // Act
        var tree = new CatalogFolderTree([shown, hidden, nestedHidden]);

        // Assert
        Assert.Multiple(
            () => Assert.Equal([shown], tree.GetListedChildren(null)),
            () => Assert.Equal([shown, hidden], tree.GetChildren(null)),
            () => Assert.Empty(tree.GetListedChildren("shown")),
            () => Assert.Equal([nestedHidden], tree.GetChildren("shown")),
            () => Assert.Same(hidden, tree.Find("hidden")),
            () => Assert.Empty(tree.GetExpandableFolders()));
    }

    [Fact]
    public void Should_offer_only_the_folders_a_tree_can_collapse()
    {
        // Act
        var tree = new CatalogFolderTree([Photos, Races, Stages, Documents]);

        // Assert
        Assert.Equal([Photos, Races], tree.GetExpandableFolders());
    }
}
