namespace Forma.Blazor.DataView.Tests;

using Forma.Blazor.Components.DataView;

public sealed class DataViewGroupingTests
{
    private static readonly Product[] Products =
    [
        new(1, "Hammer", "Tools"),
        new(2, "Atlas", "Books"),
        new(3, "Wrench", "Tools"),
        new(4, "Reel", "Films"),
        new(5, "Saw", "Tools"),
    ];

    [Fact]
    public void Should_count_each_group_in_the_order_the_groups_come_in()
    {
        // Arrange
        var grouping = DataViewGrouping<Product>.By(product => product.Category);

        // Act
        var sizes = grouping.CountGroups(Products.AsQueryable()).ToList();

        // Assert
        Assert.Equal([new DataViewGroupSize("Books", 1), new DataViewGroupSize("Films", 1), new DataViewGroupSize("Tools", 3)], sizes);
    }

    [Fact]
    public void Should_count_the_groups_largest_key_first_when_grouping_descending()
    {
        // Arrange
        var grouping = DataViewGrouping<Product>.ByDescending(product => product.Category);

        // Act
        var sizes = grouping.CountGroups(Products.AsQueryable()).ToList();

        // Assert
        Assert.Equal(["Tools", "Films", "Books"], sizes.Select(size => size.Key));
    }

    [Fact]
    public void Should_refuse_to_count_groups_for_a_request_that_names_no_grouping()
    {
        // Arrange
        var request = new DataViewRequest<Product>(0, 0, null, null, null, DataViewSortDirection.Ascending, CancellationToken.None) { IncludesGroupSizes = true };

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => request.CountGroups(Products.AsQueryable()));

        // Assert
        Assert.Contains("grouping", ex.Message, StringComparison.Ordinal);
    }

    private sealed record Product(int Id, string Name, string Category);
}
