namespace Forma.Framework.Composer.Plugins;

/// <summary>A command row a plugin declares for its own items.</summary>
public sealed record ComposerToolbarRow
{
    /// <summary>The identifier of the row.</summary>
    public required string Id { get; init; }

    /// <summary>The row's accessible name, in English, used as the localization key.</summary>
    public required string Label { get; init; }
}
