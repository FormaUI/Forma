namespace Forma.UI.Blazor.Tests.Support.Shared;

using System.Globalization;

/// <summary>Switches the current culture for the life of the fixture and restores it on disposal.</summary>
public sealed class CultureFixture : IDisposable
{
    private readonly CultureInfo _culture;
    private readonly CultureInfo _uiCulture;

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
