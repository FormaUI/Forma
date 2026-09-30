namespace Forma.Framework.Composer.Syntax;

/// <summary>A syntax highlighter that classifies nothing.</summary>
public sealed class NullSyntaxHighlighter : IComposerSyntaxHighlighter
{
    /// <summary>The shared instance.</summary>
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
