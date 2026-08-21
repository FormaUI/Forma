namespace Forma.UI.Features.Composer.Editing;

/// <summary>The command names the composer engine handles itself, rather than routing to a plugin.</summary>
public static class ComposerCommands
{
    /// <summary>Steps the document back to the preceding recorded state.</summary>
    public const string Undo = "composer.undo";

    /// <summary>Steps the document forward to the following recorded state.</summary>
    public const string Redo = "composer.redo";
}
