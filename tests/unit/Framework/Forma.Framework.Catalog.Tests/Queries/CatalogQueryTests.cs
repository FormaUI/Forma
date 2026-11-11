namespace Forma.Framework.Catalog.Tests.Queries;

using Forma.Framework.Catalog.Queries;

using Xunit;

public sealed class CatalogQueryTests
{
    private static readonly CatalogQuery Query = new(null, 0, 10, null, CatalogSortField.Name, CatalogSortDirection.Ascending);

    [Fact]
    public void Should_ask_for_every_kind_where_it_names_no_kinds()
        => Assert.All(Enum.GetValues<CatalogItemKind>(), kind => Assert.True(Query.AsksFor(kind)));

    [Fact]
    public void Should_ask_for_the_kinds_it_names_alone()
    {
        // Arrange
        var query = Query with { Kinds = new HashSet<CatalogItemKind> { CatalogItemKind.Folder, CatalogItemKind.Image } };

        // Assert
        Assert.Equal(
            [CatalogItemKind.Folder, CatalogItemKind.Image],
            Enum.GetValues<CatalogItemKind>().Where(query.AsksFor));
    }

    [Fact]
    public void Should_ask_for_no_kind_where_its_kinds_are_empty()
    {
        // Arrange
        var query = Query with { Kinds = new HashSet<CatalogItemKind>() };

        // Assert
        Assert.DoesNotContain(Enum.GetValues<CatalogItemKind>(), query.AsksFor);
    }
}
