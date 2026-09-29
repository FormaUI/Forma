namespace Forma.Blazor.Components.DataView;

/// <summary>How many items one group of a grouped set holds, which a virtualizing data view reads to place each group's heading before its items are fetched.</summary>
/// <param name="Key">The key the group's items share.</param>
/// <param name="Count">How many items the group holds.</param>
public sealed record DataViewGroupSize(object? Key, int Count);
