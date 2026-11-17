namespace Forma.Framework.Localization;

/// <summary>The checks localization runs on every lookup, reporting each key it finds no entry for in the culture the lookup is made in and in <see cref="Cultures"/>.</summary>
public sealed record LocalizationDiagnostics
{
    /// <summary>The cultures every lookup is checked in beside the culture it is made in, each named as .NET names it.</summary>
    public IReadOnlyList<string> Cultures { get; init; } = [];
}
