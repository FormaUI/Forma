namespace Forma.Features.Composer.Plugins;

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

    /// <summary>The combination written for a tooltip, such as Ctrl+Shift+Z.</summary>
    public string Display
        => Write("Ctrl");

    /// <summary>The combination written for aria-keyshortcuts, whose modifier names the specification fixes.</summary>
    public string AriaKeyShortcuts
        => Write("Control");

    private string Write(string control)
        => string.Concat(
            Control ? control + "+" : null,
            Alt ? "Alt+" : null,
            Shift ? "Shift+" : null,
            Key.Length == 1 ? Key.ToUpperInvariant() : Key);
}
