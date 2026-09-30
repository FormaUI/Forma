namespace Forma.Framework.Composer.Editing;

/// <summary>The identifiers of the command rows the composer provides.</summary>
public static class ComposerRows
{
    /// <summary>The shared row carrying the composer's own commands and the active editor's unrouted ones.</summary>
    public const string BuiltIn = "composer.built-in";

    /// <summary>The row carrying the unrouted commands of the plugins scoped to the active editor.</summary>
    public const string Plugins = "composer.plugins";

    /// <summary>The row carrying the commands that style the content of the active editor.</summary>
    public const string Formatting = "composer.formatting";

    /// <summary>The row carrying the commands that arrange the blocks of the active editor.</summary>
    public const string Paragraph = "composer.paragraph";

    /// <summary>The bar shown over a text selection in the active editor.</summary>
    public const string Selection = "composer.selection";
}
