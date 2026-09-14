namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

/// <summary>Options configuring table creation and limits in rich text documents.</summary>
public sealed record TablePluginOptions
{
    /// <summary>The maximum number of rows allowed when creating a table.</summary>
    public int MaxRows { get; init; } = 20;

    /// <summary>The maximum number of columns allowed when creating a table.</summary>
    public int MaxColumns { get; init; } = 10;

    /// <summary>Default number of rows when inserting a new table.</summary>
    public int DefaultRows { get; init; } = 3;

    /// <summary>Default number of columns when inserting a new table.</summary>
    public int DefaultColumns { get; init; } = 3;

    /// <summary>Whether a header row is included by default.</summary>
    public bool IncludeHeader { get; init; } = true;

    /// <summary>Default options for the table plugin.</summary>
    public static TablePluginOptions Default { get; } = new();
}
