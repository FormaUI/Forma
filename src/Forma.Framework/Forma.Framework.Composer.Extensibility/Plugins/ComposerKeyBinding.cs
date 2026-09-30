namespace Forma.Framework.Composer.Plugins;

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
        => string.Join('+', Write("Ctrl"));

    /// <summary>The keys of the combination, in the order a tooltip writes them, such as Ctrl, Shift and Z.</summary>
    public IReadOnlyList<string> DisplayKeys
        => [.. Write("Ctrl")];

    /// <summary>The combination written for aria-keyshortcuts, such as Control+Shift+Z.</summary>
    public string AriaKeyShortcuts
        => string.Join('+', Write("Control"));

    private IEnumerable<string> Write(string control)
    {
        if (Control)
        {
            yield return control;
        }

        if (Alt)
        {
            yield return "Alt";
        }

        if (Shift)
        {
            yield return "Shift";
        }

        yield return Key.Length == 1 ? Key.ToUpperInvariant() : Key;
    }
}
