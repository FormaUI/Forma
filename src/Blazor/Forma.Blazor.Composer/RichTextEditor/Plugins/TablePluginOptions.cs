namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

/// <summary>Options configuring table creation and limits in rich text documents.</summary>
public sealed record TablePluginOptions
{
    /// <summary>The maximum number of rows allowed when creating a table.</summary>
    public int MaxRowCount { get; set; } = 20;

    /// <summary>The maximum number of columns allowed when creating a table.</summary>
    public int MaxColumnCount { get; set; } = 10;

    /// <summary>Default number of rows when inserting a new table.</summary>
    public int DefaultRowCount { get; set; } = 3;

    /// <summary>Default number of columns when inserting a new table.</summary>
    public int DefaultColumnCount { get; set; } = 3;

    /// <summary>Whether a header row is included by default.</summary>
    public bool IncludeHeader { get; set; } = true;

    /// <summary>Default options for the table plugin.</summary>
    public static TablePluginOptions Default => new();
}
