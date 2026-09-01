namespace Forma.Features.Explorer.Tests;

using Forma.Framework.Resources;

using Xunit;

public sealed class FolderDraftTests
{
    [Fact]
    public void Should_open_fresh_under_the_given_parent()
    {
        // Arrange
        var draft = new FolderDraft { Name = "left over" };

        // Act
        var parent = new FolderResourceInfo("/photos");
        draft.Open(parent);

        // Assert
        Assert.Multiple(
            () => Assert.True(draft.IsOpen),
            () => Assert.Same(parent, draft.Parent),
            () => Assert.Equal(string.Empty, draft.Name));
    }

    [Fact]
    public void Should_create_only_with_a_usable_name()
    {
        // Arrange
        var draft = new FolderDraft();
        draft.Open(parent: null);

        // Act & Assert
        Assert.False(draft.CanCreate);

        draft.Name = "  Races  ";
        Assert.Multiple(
            () => Assert.True(draft.CanCreate),
            () => Assert.Equal("Races", draft.ConfirmedName));
    }

    [Fact]
    public void Should_hold_nothing_once_closed()
    {
        // Arrange
        var draft = new FolderDraft();
        draft.Open(new FolderResourceInfo("/photos"));
        draft.Name = "Races";

        // Act
        draft.Close();

        // Assert
        Assert.Multiple(
            () => Assert.False(draft.IsOpen),
            () => Assert.Null(draft.Parent),
            () => Assert.Equal(string.Empty, draft.Name),
            () => Assert.False(draft.CanCreate));
    }
}
