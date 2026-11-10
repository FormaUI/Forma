namespace Forma.Framework.Catalog.Queries;

/// <summary>How many items of a whole query fall in a group.</summary>
/// <param name="Key">The group's key.</param>
/// <param name="Count">How many items the group holds.</param>
public sealed record CatalogGroupSize(string Key, int Count);
