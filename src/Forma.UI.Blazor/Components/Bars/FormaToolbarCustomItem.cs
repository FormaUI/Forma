namespace Forma.UI.Blazor.Components.Bars;

/// <summary>A <see cref="FormaToolbarItem"/> rendered by a caller-supplied component.</summary>
public sealed record FormaToolbarCustomItem : FormaToolbarItem
{
    /// <summary>The component type rendered in place of a standard toolbar control.</summary>
    public required Type Component { get; init; }

    /// <summary>Parameters passed to the component, or null when it takes none.</summary>
    public IReadOnlyDictionary<string, object>? Parameters { get; init; }
}
