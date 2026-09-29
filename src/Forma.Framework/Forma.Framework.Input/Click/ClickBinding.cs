namespace Forma.Framework.Input.Click;

/// <summary>A click binding: a press and release on one element, or a control's activation from the keyboard, matched by button and only the modifiers it states.</summary>
public sealed record ClickBinding : InputBinding
{
    /// <summary>The button clicked: 0 for the primary; <see langword="null"/> matches any.</summary>
    public int? Button { get; init; }

    /// <summary>The modifiers the click must be made with; only those stated are checked.</summary>
    public ModifierRequirement Modifiers { get; init; } = ModifierRequirement.None;

    /// <summary>Whether the binding matches a double click rather than a single one.</summary>
    public bool IsDouble { get; init; }

    /// <inheritdoc />
    public override string Type
        => IsDouble ? "dblclick" : "click";
}
