namespace Forma.Framework.Localization.Parser.Syntax;

/// <summary>What reading a locale path produced: its runs, and what was found wrong with it.</summary>
/// <param name="Parts">The runs, in the order the path writes them.</param>
/// <param name="Diagnostics">What was found wrong, empty when the path is well formed.</param>
public sealed record LocalePathResult(IReadOnlyList<LocalePathPart> Parts, IReadOnlyList<LocaleDiagnostic> Diagnostics)
{
    /// <summary>Whether the path is well formed.</summary>
    public bool IsValid
        => Diagnostics.Count == 0;
}
