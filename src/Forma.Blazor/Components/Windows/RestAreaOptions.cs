namespace Forma.Blazor.Components.Windows;

/// <summary>How a <see cref="FormaRestArea"/> arranges and clusters its handles.</summary>
public sealed record RestAreaOptions
{
    /// <summary>The defaults: one wrapping row of handles, ungrouped.</summary>
    public static RestAreaOptions Default { get; } = new();

    /// <summary>How the handles are arranged.</summary>
    public RestAreaLayout Layout { get; init; } = RestAreaLayout.Row;

    /// <summary>How the handles are clustered.</summary>
    public RestAreaGrouping Grouping { get; init; } = RestAreaGrouping.None;
}
