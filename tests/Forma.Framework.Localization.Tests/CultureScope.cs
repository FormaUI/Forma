namespace Forma.Framework.Localization.Tests;

/// <summary>Sets the UI culture for the length of a test and restores it afterwards.</summary>
internal sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _previous = CultureInfo.CurrentUICulture;

    public CultureScope(string culture)
    {
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
    }

    public void Dispose()
        => CultureInfo.CurrentUICulture = _previous;
}
