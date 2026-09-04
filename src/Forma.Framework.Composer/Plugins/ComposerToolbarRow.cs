namespace Forma.Framework.Composer.Plugins;

/// <summary>A command row a plugin declares for its own items, laid out after the rows the composer provides.</summary>
public sealed record ComposerToolbarRow
{
    /// <summary>The identifier toolbar items name to be laid out in this row.</summary>
    public required string Id { get; init; }

    /// <summary>The row's accessible name, in English, used as the localization key.</summary>
    public required string Label { get; init; }
}
