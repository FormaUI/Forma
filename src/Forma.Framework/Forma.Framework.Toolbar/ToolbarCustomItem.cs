namespace Forma.Framework.Toolbar;

/// <summary>A <see cref="ToolbarItem"/> rendered by a caller-supplied component.</summary>
public sealed record ToolbarCustomItem : ToolbarItem
{
    /// <summary>The component type rendered in place of a standard toolbar control.</summary>
    public required Type Component { get; init; }

    /// <summary>The parameters passed to the component.</summary>
    public IReadOnlyDictionary<string, object>? Parameters { get; init; }
}
