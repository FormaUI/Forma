namespace Forma.UI.Features.Composer.Plugins;

/// <summary>A keyboard shortcut a plugin contributes to the composer.</summary>
public sealed record ComposerKeyBinding
{
    /// <summary>The key that completes the combination.</summary>
    public required string Key { get; init; }

    /// <summary>The command invoked when the combination is pressed.</summary>
    public required string Command { get; init; }

    /// <summary>Whether Ctrl, or Cmd on macOS, is part of the combination.</summary>
    public bool Control { get; init; }

    /// <summary>Whether Alt, or Option on macOS, is part of the combination.</summary>
    public bool Alt { get; init; }

    /// <summary>Whether Shift is part of the combination.</summary>
    public bool Shift { get; init; }
}
