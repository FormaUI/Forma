namespace Forma.Framework.Localization.Parser.Syntax;

/// <summary>A parsed expression, such as <c>[$ [count] plural; one: # file; other: # files; $]</c>.</summary>
/// <param name="Span">The range covering the expression including its delimiters.</param>
/// <param name="KeywordSpan">The range covering the keyword.</param>
/// <param name="ArgumentSpan">The range covering the argument name, brackets included.</param>
/// <param name="NameSpan">The range covering the argument name alone.</param>
/// <param name="Fields">The fields, in the order they were written.</param>
public sealed record LocaleExpression(
    LocaleTextSpan Span,
    LocaleTextSpan KeywordSpan,
    LocaleTextSpan ArgumentSpan,
    LocaleTextSpan NameSpan,
    IReadOnlyList<LocaleField> Fields);
