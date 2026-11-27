namespace Forma.Blazor.Components.Themes;

using Forma.Framework.Localization;
using Forma.Framework.Themes;

/// <summary>Forma's own themes, which the component library registers for every host: Forma and Forma High Contrast, each in both schemes.</summary>
public static class FormaThemes
{
    /// <summary>Forma, the library's look, in <c>forma-tokens.css</c>.</summary>
    public static ThemeDefinition Forma { get; } = new(
        "forma",
        LocalizableText.For<FormaThemePicker>("Forma"),
        ColorSchemes.Light | ColorSchemes.Dark,
        "_content/Forma.Blazor/forma-tokens.css");

    /// <summary>Forma High Contrast, for readers who need more contrast than Forma's gives, in <c>forma-high-contrast.css</c>.</summary>
    public static ThemeDefinition HighContrast { get; } = new(
        "forma-high-contrast",
        LocalizableText.For<FormaThemePicker>("Forma High Contrast"),
        ColorSchemes.Light | ColorSchemes.Dark,
        "_content/Forma.Blazor/forma-high-contrast.css");

    // Registers Forma's two themes before any host's own configuration.
    internal static void Register(ThemeOptions options)
    {
        options.Add(Forma);
        options.Add(HighContrast);
    }
}
