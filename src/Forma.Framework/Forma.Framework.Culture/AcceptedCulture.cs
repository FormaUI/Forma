namespace Forma.Framework.Culture;

using System.Globalization;

/// <summary>A culture pair a switch accepted, with the version it accepted the pair under.</summary>
public sealed record AcceptedCulture
{
    /// <summary>Creates the accepted pair of <paramref name="culture"/> and <paramref name="uiCulture"/> under <paramref name="version"/>.</summary>
    /// <param name="culture">The culture that formats dates, numbers and collation.</param>
    /// <param name="uiCulture">The culture that chooses strings and direction.</param>
    /// <param name="version">The version the switch accepted the pair under.</param>
    public AcceptedCulture(CultureInfo culture, CultureInfo uiCulture, CultureVersion version)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(uiCulture);
        ArgumentNullException.ThrowIfNull(version);

        Culture = culture;
        UICulture = uiCulture;
        Version = version;
    }

    /// <summary>The culture that formats dates, numbers and collation.</summary>
    public CultureInfo Culture { get; }

    /// <summary>The culture that chooses strings and direction.</summary>
    public CultureInfo UICulture { get; }

    /// <summary>The version the switch accepted the pair under.</summary>
    public CultureVersion Version { get; }
}
