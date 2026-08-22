namespace Forma.UI.Features.Composer.Plugins;

/// <summary>One option inside a toolbar item's menu.</summary>
public sealed record ComposerToolbarOption
{
    /// <summary>The unique identifier of the choice within its item.</summary>
    public required string Id { get; init; }

    /// <summary>The choice's label, in English, used as the localization key and accessible name.</summary>
    public required string Title { get; init; }

    /// <summary>The command invoked when the choice is picked.</summary>
    public required string Command { get; init; }

    /// <summary>Inline CSS previewing the choice's effect on its own label, or null.</summary>
    public string? PreviewStyle { get; init; }

    /// <summary>A CSS color rendered as a swatch beside the label, or null.</summary>
    public string? Swatch { get; init; }
}
