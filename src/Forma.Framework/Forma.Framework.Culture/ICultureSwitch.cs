namespace Forma.Framework.Culture;

using System.Globalization;

/// <summary>Requests a change of the application's cultures.</summary>
public interface ICultureSwitch
{
    /// <summary>Requests the cultures <paramref name="culture"/> and <paramref name="uiCulture"/>.</summary>
    /// <param name="culture">The culture that formats dates, numbers and collation.</param>
    /// <param name="uiCulture">The culture that chooses strings and direction; <see langword="null"/> takes <paramref name="culture"/>.</param>
    /// <returns>A task that completes once the switch was accepted and the requesting runtime applied the pair or a later one.</returns>
    Task SwitchAsync(CultureInfo culture, CultureInfo? uiCulture = null);
}
