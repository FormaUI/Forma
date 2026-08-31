namespace Forma.Framework.Localization.Parser.Syntax;

/// <summary>Everything reading a standalone field list found: the fields, and what was wrong.</summary>
/// <param name="Fields">The fields, in the order they were written.</param>
/// <param name="Diagnostics">What the parser found wrong.</param>
public sealed record LocaleFieldResult(
    IReadOnlyList<LocaleField> Fields,
    IReadOnlyList<LocaleDiagnostic> Diagnostics)
{
    /// <summary>Whether the parser found nothing wrong.</summary>
    public bool IsValid
        => Diagnostics.Count == 0;
}
