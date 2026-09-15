namespace Forma.Framework.Composer.Dialogs;

using System.Collections.ObjectModel;

/// <summary>A dialog a plugin asks the composer to open, whose body the plugin supplies and whose shell the composer owns.</summary>
public sealed record ComposerInsertionRequest
{
    /// <summary>The component filling the dialog, which implements <see cref="IComposerInsertionDialog"/>.</summary>
    public required Type Body { get; init; }

    /// <summary>The parameters handed to the body.</summary>
    public IReadOnlyDictionary<string, object?> Parameters { get; init; } = ReadOnlyDictionary<string, object?>.Empty;

    /// <summary>The room the body is given.</summary>
    public ComposerDialogSize Size { get; init; } = ComposerDialogSize.Medium;
}
