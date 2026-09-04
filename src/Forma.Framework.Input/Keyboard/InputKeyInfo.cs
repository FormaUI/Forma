namespace Forma.Framework.Input.Keyboard;

/// <summary>A key press or release that raised an action.</summary>
public sealed record InputKeyInfo : InputInfo
{
    /// <summary>The key.</summary>
    public required string Key { get; init; }

    /// <summary>The physical key.</summary>
    public string? Code { get; init; }

    /// <summary>The modifiers held.</summary>
    public ModifierKeys Modifiers { get; init; } = new();
}
