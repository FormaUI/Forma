namespace Forma.Framework.Explorer.Tests.Items;

using Forma.Framework.Explorer.Items;
using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerItemExtensionsTests
{
    private const ExplorerItemCapabilities EveryMediaCapability =
        ExplorerItemCapabilities.Drag
        | ExplorerItemCapabilities.Open
        | ExplorerItemCapabilities.View
        | ExplorerItemCapabilities.Move
        | ExplorerItemCapabilities.Rename
        | ExplorerItemCapabilities.Delete;

    private static ExplorerItem CreateItem(ExplorerItemCapabilities capabilities, bool isDisabled = false)
        => new()
        {
            Key = "/media/photo.png",
            Name = "photo.png",
            Capabilities = capabilities,
            IsDisabled = isDisabled,
            Resource = new FileResourceInfo("/media/photo.png")
        };

    [Fact]
    public void Should_permit_only_what_both_the_item_and_the_options_allow()
    {
        // Arrange
        var options = new ExplorerItemOptions
        {
            AllowedCapabilities = ExplorerItemCapabilities.Open | ExplorerItemCapabilities.View | ExplorerItemCapabilities.Rename
        };
        var item = CreateItem(ExplorerItemCapabilities.Open | ExplorerItemCapabilities.Rename | ExplorerItemCapabilities.Delete);

        // Act
        var capabilities = item.GetCapabilities(options);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ExplorerItemCapabilities.Open | ExplorerItemCapabilities.Rename, capabilities),
            () => Assert.True(item.Permits(options, ExplorerItemCapabilities.Rename)),
            () => Assert.False(item.Permits(options, ExplorerItemCapabilities.Delete)));
    }

    [Fact]
    public void Should_gate_a_folder_by_the_folder_capabilities()
    {
        // Arrange
        var options = new ExplorerItemOptions
        {
            AllowedCapabilities = ExplorerItemCapabilities.None,
            AllowedFolderCapabilities = ExplorerItemCapabilities.Open | ExplorerItemCapabilities.Drop
        };
        var folder = ExplorerItem.ForFolder(new FolderResourceInfo("/photos"));

        // Act
        var capabilities = folder.GetCapabilities(options);

        // Assert
        Assert.Equal(ExplorerItemCapabilities.Open | ExplorerItemCapabilities.Drop, capabilities);
    }

    [Fact]
    public void Should_permit_nothing_on_a_disabled_item()
    {
        // Arrange
        var options = ExplorerItemOptions.Default;
        var item = CreateItem(EveryMediaCapability, isDisabled: true);

        // Act
        var capabilities = item.GetCapabilities(options);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ExplorerItemCapabilities.None, capabilities),
            () => Assert.False(item.Permits(options, ExplorerItemCapabilities.View)));
    }

    [Fact]
    public void Should_reject_a_null_item_or_options()
    {
        // Arrange
        var item = CreateItem(EveryMediaCapability);

        // Act & Assert
        Assert.Multiple(
            () => Assert.Throws<ArgumentNullException>(() => ((IExplorerItem)null!).GetCapabilities(ExplorerItemOptions.Default)),
            () => Assert.Throws<ArgumentNullException>(() => item.GetCapabilities(null!)));
    }
}
