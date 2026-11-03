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

    /// <summary>The embed dialog.</summary>
    Embed
}

/// <summary>The dialog an editor has open, with the values of the element it reopened on.</summary>
internal sealed record RichTextDialogState(RichTextDialogKind Kind, ComposerDialogResult? ExistingResult = null)
{
    /// <summary>No dialog open.</summary>
    public static RichTextDialogState None { get; } = new(RichTextDialogKind.None);
}
