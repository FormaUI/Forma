namespace Forma.Framework.Localization.Parser.Syntax;

/// <summary>Something a parser found wrong, and where it sits in the text.</summary>
/// <param name="Kind">What is wrong.</param>
/// <param name="Span">The range of text the finding covers.</param>
public readonly record struct LocaleDiagnostic(LocaleDiagnosticKind Kind, LocaleTextSpan Span);
