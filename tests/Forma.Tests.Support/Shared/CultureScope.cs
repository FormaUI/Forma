namespace Forma.Tests.Shared;

using System.Globalization;

/// <summary>Sets the UI culture and restores the previous one on disposal.</summary>
public sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _previous = CultureInfo.CurrentUICulture;

    /// <summary>Switches the UI culture to the named one.</summary>
    /// <param name="culture">The name of the culture.</param>
    public CultureScope(string culture)
    {
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
    }

    /// <inheritdoc />
    public void Dispose()
        => CultureInfo.CurrentUICulture = _previous;
}
