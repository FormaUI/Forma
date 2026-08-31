namespace Forma.Framework.Localization.Parser.Syntax;

/// <summary>One <c>name: value</c> pair, such as a plural category or a metadata entry.</summary>
/// <param name="NameSpan">The range covering the name.</param>
/// <param name="ValueSpan">The range covering the value.</param>
public readonly record struct LocaleField(LocaleTextSpan NameSpan, LocaleTextSpan ValueSpan);
