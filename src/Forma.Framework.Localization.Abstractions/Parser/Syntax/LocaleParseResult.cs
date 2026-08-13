namespace Forma.Framework.Localization.Parser.Syntax;

/// <summary>Everything a parse found: the expressions that were read, and what was wrong.</summary>
/// <param name="Expressions">The expressions, in the order they appear.</param>
/// <param name="Diagnostics">What the parser found wrong.</param>
public sealed record LocaleParseResult(
    IReadOnlyList<LocaleExpression> Expressions,
    IReadOnlyList<LocaleDiagnostic> Diagnostics)
{
    /// <summary>A result for text that carries no expression.</summary>
    public static LocaleParseResult Empty { get; } = new([], []);

    /// <summary>Whether the parser found nothing wrong.</summary>
    public bool IsValid
        => Diagnostics.Count == 0;
}
