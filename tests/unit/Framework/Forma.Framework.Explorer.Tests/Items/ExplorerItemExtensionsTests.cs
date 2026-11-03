namespace Forma.Framework.Explorer.Tests.Items;

using Forma.Framework.Catalog;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerItemExtensionsTests
{
    private const CatalogItemCapabilities EveryMediaCapability =
        CatalogItemCapabilities.Drag
        | CatalogItemCapabilities.Open
        | CatalogItemCapabilities.View
        | CatalogItemCapabilities.Move
        | CatalogItemCapabilities.Rename
        | CatalogItemCapabilities.Delete;

    private static CatalogItem CreateItem(CatalogItemCapabilities capabilities, bool isDisabled = false)
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
            AllowedCapabilities = CatalogItemCapabilities.Open | CatalogItemCapabilities.View | CatalogItemCapabilities.Rename
        };
        var item = CreateItem(CatalogItemCapabilities.Open | CatalogItemCapabilities.Rename | CatalogItemCapabilities.Delete);

        // Act
        var capabilities = item.GetCapabilities(options);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(CatalogItemCapabilities.Open | CatalogItemCapabilities.Rename, capabilities),
            () => Assert.True(item.Permits(options, CatalogItemCapabilities.Rename)),
            () => Assert.False(item.Permits(options, CatalogItemCapabilities.Delete)));
    }

    [Fact]
    public void Should_gate_a_folder_by_the_folder_capabilities()
    {
        // Arrange
        var options = new ExplorerItemOptions
        {
            AllowedCapabilities = CatalogItemCapabilities.None,
            AllowedFolderCapabilities = CatalogItemCapabilities.Open | CatalogItemCapabilities.Drop
        };
        var folder = CatalogItem.ForFolder(new FolderResourceInfo("/photos"));

        // Act
        var capabilities = folder.GetCapabilities(options);

        // Assert
        Assert.Equal(CatalogItemCapabilities.Open | CatalogItemCapabilities.Drop, capabilities);
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
            () => Assert.Equal(CatalogItemCapabilities.None, capabilities),
            () => Assert.False(item.Permits(options, CatalogItemCapabilities.View)));
    }

    [Fact]
    public void Should_reject_a_null_item_or_options()
    {
        // Arrange
        var item = CreateItem(EveryMediaCapability);

        // Act & Assert
        Assert.Multiple(
            () => Assert.Throws<ArgumentNullException>(() => ((ICatalogItem)null!).GetCapabilities(ExplorerItemOptions.Default)),
            () => Assert.Throws<ArgumentNullException>(() => item.GetCapabilities(null!)));
    }
}
