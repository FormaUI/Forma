namespace Forma.Framework.Composer.Syntax;

/// <summary>A highlighter classifying nothing, standing in where a host registered none.</summary>
public sealed class NullSyntaxHighlighter : IComposerSyntaxHighlighter
{
    /// <summary>The one instance, since a highlighter that classifies nothing has no state to vary.</summary>
    public static NullSyntaxHighlighter Instance { get; } = new();

    private NullSyntaxHighlighter()
    {
    }

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedLanguages
        => [];

    /// <inheritdoc />
    public IReadOnlyList<ComposerSyntaxToken> Tokenize(string code, string language)
        => [];
}
