namespace Forma.Tests.Shared;

using System.Globalization;

/// <summary>Restores the current culture and UI culture on disposal, and can switch them for the life of the fixture.</summary>
public sealed class CultureFixture : IDisposable
{
    private readonly CultureInfo _culture;
    private readonly CultureInfo _uiCulture;

    /// <summary>Captures the current culture and leaves it in place.</summary>
    public CultureFixture()
    {
        _culture = CultureInfo.CurrentCulture;
        _uiCulture = CultureInfo.CurrentUICulture;
    }

    /// <summary>Captures the current culture and switches to the named one.</summary>
    public CultureFixture(string name)
        : this(GetNamedCulture(name))
    {
    }

    /// <summary>Captures the current culture and switches to <paramref name="culture"/>.</summary>
    public CultureFixture(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        _culture = CultureInfo.CurrentCulture;
        _uiCulture = CultureInfo.CurrentUICulture;

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }

    private static CultureInfo GetNamedCulture(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return CultureInfo.GetCultureInfo(name);
    }
}
