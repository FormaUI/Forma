namespace Forma.UI.Features.Composer.Editing;

/// <summary>A live status readout a plugin has pushed to the composer's status strip, mutable for as long as it is held.</summary>
public interface IComposerStatusItem : IDisposable
{
    /// <summary>The text shown for this item.</summary>
    string Text { get; set; }

    /// <summary>The tooltip shown on hover, or null for none.</summary>
    string? Tooltip { get; set; }

    /// <summary>The command run when the item is activated, or null when it is not interactive.</summary>
    string? Command { get; set; }

    /// <summary>Whether a screen reader announces this item's text as it changes; false for a readout that changes too often to be worth interrupting for.</summary>
    bool IsAnnounced { get; set; }
}
