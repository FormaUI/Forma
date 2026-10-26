namespace Forma.Blazor.Components;

using System.Globalization;

using Forma.Framework.Globalization;
using Forma.Framework.Localization;

/// <summary>A locale that looks its strings up in a culture state's cultures, bound again whenever the state changes.</summary>
/// <param name="locale">The locale whose strings are looked up.</param>
/// <param name="cultureState">The culture state whose cultures the strings are looked up in.</param>
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
