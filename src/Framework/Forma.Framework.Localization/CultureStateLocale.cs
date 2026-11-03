namespace Forma.Framework.Localization;

using System.Globalization;

using Forma.Framework.Culture;

/// <summary>Resolves localized strings using the cultures of an <see cref="ICultureState"/>.</summary>
/// <remarks>
/// Creates a culture-bound locale on first use and refreshes it on the first use after
/// the culture state's revision changes. The bound locale and its revision are published as one binding,
/// and the revision is read before the cultures, so a change landing between the reads leaves a binding
/// whose cultures are newer than its revision, which the next use replaces, and never one whose cultures
/// are older. Several threads may read the locale while the state changes.
/// </remarks>
/// <param name="locale">The underlying locale to bind to the state's cultures.</param>
/// <param name="cultureState">The state that supplies the formatting and UI cultures.</param>
internal sealed class CultureStateLocale(IStringLocale locale, ICultureState cultureState) : IStringLocale
{
    private Binding? _binding;

    private IStringLocale BoundLocale
    {
        get
        {
            var binding = Volatile.Read(ref _binding);
            var revision = cultureState.Revision;

            if (binding is null || binding.Revision != revision)
            {
                binding = new Binding(locale.WithCulture(cultureState.Culture, cultureState.UICulture), revision);
                Volatile.Write(ref _binding, binding);
            }

            return binding.Locale;
        }
    }

    /// <inheritdoc />
    public LocalizedText this[string name]
        => BoundLocale[name];

    /// <inheritdoc />
    public LocalizedText this[string name, params ReadOnlySpan<object?> values]
        => BoundLocale[name, values];

    /// <inheritdoc />
    public LocalizedPattern Resolve(string name)
        => BoundLocale.Resolve(name);

    /// <inheritdoc />
    public LocalizedPattern Resolve(string name, params object?[] values)
        => BoundLocale.Resolve(name, values);

    /// <inheritdoc />
    public IEnumerable<LocalizedText> GetAllStrings(bool includeParentCultures)
        => BoundLocale.GetAllStrings(includeParentCultures);

    /// <inheritdoc />
    public IStringLocale WithCulture(CultureInfo culture, CultureInfo uiCulture)
        => locale.WithCulture(culture, uiCulture);

    /// <summary>A locale bound to the state's cultures, with the revision read before them.</summary>
    /// <param name="Locale">The locale bound to the state's cultures.</param>
    /// <param name="Revision">The state's revision, read before its cultures.</param>
    private sealed record Binding(IStringLocale Locale, int Revision);
}
