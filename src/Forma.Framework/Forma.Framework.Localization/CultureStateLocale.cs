namespace Forma.Framework.Localization;

using System.Globalization;

using Forma.Framework.Globalization;

/// <summary>Resolves localized strings using the cultures of an <see cref="ICultureState"/>.</summary>
/// <remarks>
/// Creates a culture-bound locale on first use and refreshes it on the first use after
/// the culture state's revision changes.
/// </remarks>
/// <param name="locale">The underlying locale to bind to the state's cultures.</param>
/// <param name="cultureState">The state that supplies the formatting and UI cultures.</param>
internal sealed class CultureStateLocale(IStringLocale locale, ICultureState cultureState) : IStringLocale
{
    private IStringLocale? _boundLocale;
    private int _boundRevision;

    private IStringLocale BoundLocale
    {
        get
        {
            if (_boundLocale is null || _boundRevision != cultureState.Revision)
            {
                _boundLocale = locale.WithCulture(cultureState.Culture, cultureState.UICulture);
                _boundRevision = cultureState.Revision;
            }

            return _boundLocale;
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
}
