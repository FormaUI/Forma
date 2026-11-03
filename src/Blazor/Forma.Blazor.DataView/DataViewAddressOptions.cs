namespace Forma.Blazor.Components.DataView;

/// <summary>Where a data view writes its sort, page and view in the address: the prefix its parameters are written under, and their names.</summary>
/// <param name="Prefix">The prefix every parameter is written under.</param>
public sealed record DataViewAddressOptions(string Prefix)
{
    /// <summary>The parameter naming the sort.</summary>
    public string SortName { get; init; } = "sort";

    /// <summary>The parameter naming the sort's direction.</summary>
    public string OrderName { get; init; } = "order";

    /// <summary>The parameter naming the page.</summary>
    public string PageName { get; init; } = "page";

    /// <summary>The parameter naming the active view.</summary>
    public string ViewName { get; init; } = "view";

    /// <summary>The full name of a parameter, the prefix and the name joined by a dash.</summary>
    /// <param name="name">The parameter's own name.</param>
    public string GetParameterName(string name)
        => $"{Prefix}-{name}";
}
