namespace Forma.Features.Explorer.Tests;

using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerFolderTreeTests
{
    private static readonly FolderResourceInfo Photos = new("/photos");
    private static readonly FolderResourceInfo Races = new("/photos/races") { Parent = "/photos" };
    private static readonly FolderResourceInfo Stages = new("/photos/races/stages") { Parent = "/photos/races" };
    private static readonly FolderResourceInfo Documents = new("/documents");

    [Fact]
    public void Should_hold_nothing_when_given_no_folders()
    {
        // Assert
        Assert.Multiple(
            () => Assert.Empty(ExplorerFolderTree.Empty.ChildrenOf(null)),
            () => Assert.Empty(new ExplorerFolderTree(null).ChildrenOf(null)));
    }

    [Fact]
    public void Should_treat_a_folder_naming_no_parent_as_top_level()
    {
        // Act
        var tree = new ExplorerFolderTree([Photos, Races, Documents]);

        // Assert
        Assert.Equal([Photos, Documents], tree.ChildrenOf(null));
    }

    [Fact]
    public void Should_find_the_folders_directly_inside_a_parent()
    {
        // Act
        var tree = new ExplorerFolderTree([Photos, Races, Stages]);

        // Assert
        Assert.Multiple(
            () => Assert.Equal([Races], tree.ChildrenOf("/photos")),
            () => Assert.Equal([Stages], tree.ChildrenOf("/photos/races")),
            () => Assert.Empty(tree.ChildrenOf("/photos/races/stages")));
    }

    [Fact]
    public void Should_hold_nothing_for_a_parent_the_tree_does_not_know()
    {
        // Act
        var tree = new ExplorerFolderTree([Photos]);

        // Assert
        Assert.Empty(tree.ChildrenOf("/nowhere"));
    }

    [Fact]
    public void Should_find_a_folder_by_its_source()
    {
        // Act
        var tree = new ExplorerFolderTree([Photos, Races]);

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
        var repeated = new FolderResourceInfo("/photos") { Name = "second" };

        // Act
        var tree = new ExplorerFolderTree([Photos, repeated]);

        // Assert
        Assert.Equal(Photos, tree.Find("/photos"));
    }

    [Fact]
    public void Should_tell_a_folder_holding_others_from_an_empty_one()
    {
        // Act
        var tree = new ExplorerFolderTree([Photos, Races]);

        // Assert
        Assert.Multiple(
            () => Assert.True(tree.HasChildren("/photos")),
            () => Assert.False(tree.HasChildren("/photos/races")));
    }

    [Fact]
    public void Should_offer_only_the_folders_a_tree_can_collapse()
    {
        // Act
        var tree = new ExplorerFolderTree([Photos, Races, Stages, Documents]);

        // Assert
        Assert.Equal([Photos, Races], tree.Expandable());
    }
}
