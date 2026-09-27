namespace Forma.Framework.Composer.Editing;

using Forma.Framework.Icons;

/// <summary>A live marker a plugin has pushed to the composer's gutter, mutable for as long as it is held.</summary>
public interface IComposerGutterMarker : IDisposable
{
    /// <summary>The zero-based block the marker sits beside, counted over the surface's top-level blocks; a line-oriented surface counts a line as a block.</summary>
    int Block { get; set; }

    /// <summary>The icon drawn for this marker.</summary>
    Icon Icon { get; set; }

    /// <summary>The tooltip shown on hover, or null for none.</summary>
    string? Tooltip { get; set; }

    /// <summary>The command run when the marker is activated, or null when it is not interactive.</summary>
    string? Command { get; set; }
}
