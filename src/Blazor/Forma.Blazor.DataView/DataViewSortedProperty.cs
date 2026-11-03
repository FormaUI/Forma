namespace Forma.Blazor.Components.DataView;

/// <summary>One key of an applied sort: the member it reads and the direction it applies in.</summary>
/// <param name="PropertyName">The member path the key reads, such as <c>Address.City</c>; empty for a key that reads no member.</param>
/// <param name="Direction">The direction the key applies in.</param>
public readonly record struct DataViewSortedProperty(string PropertyName, DataViewSortDirection Direction);
