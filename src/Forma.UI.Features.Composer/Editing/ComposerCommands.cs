namespace Forma.UI.Features.Composer.Editing;

/// <summary>The command names the composer engine handles itself, rather than routing to a plugin.</summary>
public static class ComposerCommands
{
    /// <summary>Steps the document back to the preceding recorded state.</summary>
    public const string Undo = "composer.undo";

    /// <summary>Steps the document forward to the following recorded state.</summary>
    public const string Redo = "composer.redo";

    /// <summary>Toggles full-viewport maximized editing mode.</summary>
    public const string Maximize = "view.maximize";

    /// <summary>Toggles the formatting bar that follows the text selection.</summary>
    public const string SelectionBar = "view.selection-bar";

    /// <summary>Opens the dialog that uploads files and attaches them to the document.</summary>
    public const string Attach = "attachment.upload";
}
