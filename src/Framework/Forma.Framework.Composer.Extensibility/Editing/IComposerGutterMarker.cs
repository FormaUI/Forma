namespace Forma.Framework.Composer.Editing;

using Forma.Framework.Icons;

/// <summary>A live marker a plugin has pushed to the composer's gutter.</summary>
public interface IComposerGutterMarker : IDisposable
{
    /// <summary>The zero-based index of the top-level block the marker sits beside.</summary>
    /// <remarks>A line-oriented surface counts each line as a block.</remarks>
    int Block { get; set; }

    /// <summary>The icon drawn for this marker.</summary>
    Icon Icon { get; set; }

    /// <summary>The tooltip shown on hover.</summary>
    string? Tooltip { get; set; }

    /// <summary>The command run when the marker is activated.</summary>
    string? Command { get; set; }
}
