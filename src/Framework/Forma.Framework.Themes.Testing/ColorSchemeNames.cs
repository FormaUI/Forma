namespace Forma.Framework.Themes.Testing;

// The name CSS gives each color scheme, as data-forma-scheme takes it.
internal static class ColorSchemeNames
{
    public static string Of(ColorSchemes scheme)
        => scheme switch
        {
            ColorSchemes.Light => "light",
            ColorSchemes.Dark => "dark",
            _ => throw new ArgumentOutOfRangeException(nameof(scheme), scheme, "A color is drawn in one scheme, Light or Dark."),
        };
}
