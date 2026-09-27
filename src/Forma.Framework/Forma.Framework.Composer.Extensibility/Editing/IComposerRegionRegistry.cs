namespace Forma.Framework.Composer.Editing;

using Forma.Framework.Icons;

/// <summary>The status items and gutter markers plugins have pushed, shared across whichever editor is active.</summary>
/// <remarks>Every member must be called from the composer's own dispatcher thread; a plugin updating a handle from a timer or background continuation must first marshal onto it.</remarks>
public interface IComposerRegionRegistry
{
    /// <summary>Every status item currently pushed, in the order each was created.</summary>
    IReadOnlyList<IComposerStatusItem> StatusItems { get; }

    /// <summary>Every gutter marker currently pushed, in the order each was created.</summary>
    IReadOnlyList<IComposerGutterMarker> GutterMarkers { get; }

    /// <summary>Raised whenever a status item or gutter marker is created, changed, or disposed.</summary>
    event Action? Changed;

    /// <summary>Pushes a status item, returning the handle that keeps it visible until disposed.</summary>
    IComposerStatusItem CreateStatusItem(string text, string? tooltip = null, string? command = null);

    /// <summary>Pushes a gutter marker beside a block, returning the handle that keeps it visible until disposed.</summary>
    IComposerGutterMarker CreateGutterMarker(int block, Icon icon, string? tooltip = null, string? command = null);
}
