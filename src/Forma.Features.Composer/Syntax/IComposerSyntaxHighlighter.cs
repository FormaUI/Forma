namespace Forma.Features.Composer.Syntax;

/// <summary>Classifies source code into scoped tokens.</summary>
public interface IComposerSyntaxHighlighter
{
    /// <summary>Languages this highlighter classifies.</summary>
    IReadOnlyList<string> SupportedLanguages { get; }

    /// <summary>Classifies the code into tokens, in order.</summary>
    IReadOnlyList<ComposerSyntaxToken> Tokenize(string code, string language);
}
