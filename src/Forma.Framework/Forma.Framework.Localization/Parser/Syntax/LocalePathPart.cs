namespace Forma.Framework.Localization.Parser.Syntax;

/// <summary>One run of a locale path: literal text, or a token.</summary>
/// <param name="Token">The token the run names, or null when the run is literal.</param>
/// <param name="Text">The literal text, with an escaped <c>$</c> already unescaped; empty for a token.</param>
/// <param name="Span">Where the run sits in the path as written.</param>
public readonly record struct LocalePathPart(LocalePathToken? Token, string Text, LocaleTextSpan Span)
{
    /// <summary>Whether the run names a token.</summary>
    public bool IsToken
        => Token is not null;
}
