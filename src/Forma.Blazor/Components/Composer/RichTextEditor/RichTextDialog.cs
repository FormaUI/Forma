namespace Forma.Blazor.Components.Composer.RichTextEditor;

using Forma.Blazor.Components.Composer.Dialogs;

/// <summary>Which dialog a rich-text editor can have open.</summary>
internal enum RichTextDialogKind
{
    /// <summary>No dialog is open.</summary>
    None,

    /// <summary>The hyperlink or named-anchor dialog.</summary>
    Link,

    /// <summary>The table dialog.</summary>
    Table,

    /// <summary>The video dialog.</summary>
    Video,

    /// <summary>The image dialog.</summary>
    Image,

    /// <summary>The media explorer dialog.</summary>
    Explorer,

    /// <summary>The embed dialog.</summary>
    Embed
}

/// <summary>The one dialog an editor has open, holding what a reopened element already carried.</summary>
internal sealed record RichTextDialog(RichTextDialogKind Kind, ComposerDialogResult? Reopening = null)
{
    /// <summary>No dialog open.</summary>
    public static RichTextDialog None { get; } = new(RichTextDialogKind.None);
}
