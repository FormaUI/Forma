namespace Forma.UI.Features.Composer.Editing;

using Forma.UI.Design.Icons;

/// <summary>A live marker a plugin has pushed to the composer's gutter, mutable for as long as it is held.</summary>
public interface IComposerGutterMarker : IDisposable
{
    /// <summary>The zero-based line the marker sits beside.</summary>
    int Line { get; set; }

    /// <summary>The icon drawn for this marker.</summary>
    Icon Icon { get; set; }

    /// <summary>The tooltip shown on hover, or null for none.</summary>
    string? Tooltip { get; set; }

    /// <summary>The command run when the marker is activated, or null when it is not interactive.</summary>
    string? Command { get; set; }
}
