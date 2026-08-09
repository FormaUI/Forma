namespace Forma.UI.Blazor.Tests.Indicators;

using Forma.Framework.Css;

public sealed class StatusBadgeCatalogTests
{
    private static readonly CssColor Background = CssColor.FromRgba(230, 245, 234, 0.5);
    private static readonly CssColor Foreground = CssColor.FromHex(0x1c7430);

    [Fact]
    public void Should_be_empty_initially()
    {
        // Arrange
        var catalog = new StatusBadgeCatalog();

        // Act & Assert
        Assert.Empty(catalog.Statuses);
    }

    [Fact]
    public void Should_build_from_a_collection_expression()
    {
        // Arrange & Act
        StatusBadgeCatalog catalog =
        [
            new("draft", Background, Foreground),
            new("published", Background, Foreground)
        ];

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(2, catalog.Statuses.Count);
            Assert.Equal(Background, catalog.Find("published")!.Background);
        });
    }

    [Fact]
    public void Should_build_from_a_collection_expression_argument()
    {
        // Arrange & Act
        var catalog = new StatusBadgeCatalog(
        [
            new("draft", Background, Foreground),
            new("published", Background, Foreground)
        ]);

        // Assert
        Assert.Equal(2, catalog.Statuses.Count);
    }

    [Fact]
    public void Should_return_registered_colours()
    {
        // Arrange
        var catalog = new StatusBadgeCatalog();

        // Act
        catalog.Add(new("published", Background, Foreground));

        var style = catalog.Find("published");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.NotNull(style);
            Assert.Equal(Background, style!.Background);
            Assert.Equal(Foreground, style.Foreground);
        });
    }

    [Fact]
    public void Should_find_a_status_regardless_of_casing()
    {
        // Arrange
        StatusBadgeCatalog catalog = [new("Published", Background, Foreground)];

        // Act & Assert
        Assert.NotNull(catalog.Find("PUBLISHED"));
    }

    [Fact]
    public void Should_replace_an_existing_status()
    {
        // Arrange
        StatusBadgeCatalog catalog = [new("published", Background, Foreground)];

        // Act
        catalog.Add(new("published", CssColor.FromHex(0x000000), CssColor.FromHex(0xffffff)));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Single(catalog.Statuses);
            Assert.Equal(CssColor.FromHex(0x000000), catalog.Find("published")!.Background);
        });
    }

    [Fact]
    public void Should_return_null_for_an_unknown_status()
    {
        // Arrange
        var catalog = new StatusBadgeCatalog();

        // Act & Assert
        Assert.Null(catalog.Find("nope"));
    }

    [Fact]
    public void Should_enumerate_every_registered_status()
    {
        // Arrange
        StatusBadgeCatalog catalog =
        [
            new("draft", Background, Foreground),
            new("published", Background, Foreground)
        ];

        // Act
        var names = catalog.Select(s => s.Name).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Contains("draft", names);
            Assert.Contains("published", names);
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_reject_an_invalid_name(string? name)
    {
        // Arrange
        var catalog = new StatusBadgeCatalog();

        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.ThrowsAny<ArgumentException>(() => new StatusBadgeStyle(name!, Background, Foreground));
            Assert.ThrowsAny<ArgumentException>(() => catalog.Find(name!));
        });
    }
}
