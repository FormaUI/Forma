namespace Forma.Framework.Composer.Editing;

/// <summary>A live status readout a plugin has pushed to the composer's status strip.</summary>
public interface IComposerStatusItem : IDisposable
{
    /// <summary>The text shown for this item.</summary>
    string Text { get; set; }

    /// <summary>The tooltip shown on hover.</summary>
    string? Tooltip { get; set; }

    /// <summary>The command run when the item is activated.</summary>
    string? Command { get; set; }

    /// <summary>Whether a screen reader announces this item's text as it changes.</summary>
    bool IsAnnounced { get; set; }
}
