namespace Forma.UI.Features.Composer.Editing;

/// <summary>The command rows the composer always provides, which an item lands in when it names no row of its own.</summary>
public static class ComposerRows
{
    /// <summary>The shared row carrying the composer's own commands and the active editor's unrouted ones.</summary>
    public const string BuiltIn = "composer.built-in";

    /// <summary>The row carrying the unrouted commands of the plugins scoped to the active editor.</summary>
    public const string Plugins = "composer.plugins";
}
