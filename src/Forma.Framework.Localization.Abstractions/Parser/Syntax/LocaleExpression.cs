namespace Forma.Framework.Localization.Parser.Syntax;

/// <summary>A parsed expression, such as <c>[$ [0] plural; one: # file; other: # files; $]</c>.</summary>
/// <param name="Span">The range covering the expression including its delimiters.</param>
/// <param name="KeywordSpan">The range covering the keyword.</param>
/// <param name="ArgumentSpan">The range covering the argument index, brackets included.</param>
/// <param name="ArgumentIndex">The argument the expression reads its value from.</param>
/// <param name="Fields">The fields, in the order they were written.</param>
public sealed record LocaleExpression(
    LocaleTextSpan Span,
    LocaleTextSpan KeywordSpan,
    LocaleTextSpan ArgumentSpan,
    int ArgumentIndex,
    IReadOnlyList<LocaleField> Fields);
