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
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _culture = CultureInfo.CurrentCulture;
        _uiCulture = CultureInfo.CurrentUICulture;

        var replacement = CultureInfo.GetCultureInfo(name);

        CultureInfo.CurrentCulture = replacement;
        CultureInfo.CurrentUICulture = replacement;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }
}
