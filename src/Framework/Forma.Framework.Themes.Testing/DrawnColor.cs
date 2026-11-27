namespace Forma.Framework.Themes.Testing;

using Forma.Framework.Css;

// A color as a browser draws it: sRGB channels from 0 to 1 and an opacity, laid over what is beneath it as the browser composites.
internal readonly record struct DrawnColor(double Red, double Green, double Blue, double Alpha)
{
    public static DrawnColor White { get; } = new(1, 1, 1, 1);

    // Machado, Oliveira and Fernandes' simulations of protanopia and deuteranopia at full severity, over linear sRGB.
    private static readonly double[][] Protanopia =
    [
        [0.152286, 1.052583, -0.204868],
        [0.114503, 0.786281, 0.099216],
        [-0.003882, -0.048116, 1.051998],
    ];

    private static readonly double[][] Deuteranopia =
    [
        [0.367322, 0.860646, -0.227968],
        [0.280085, 0.672501, 0.047413],
        [-0.011820, 0.042940, 0.968881],
    ];

    public static DrawnColor From(CssColor color)
        => new(color.Red / 255d, color.Green / 255d, color.Blue / 255d, color.Alpha);

    // The color source-over below, in the encoded channels a browser composites in.
    public DrawnColor Over(DrawnColor below)
    {
        var alpha = Alpha + (below.Alpha * (1 - Alpha));

        if (alpha <= 0)
        {
            return new DrawnColor(0, 0, 0, 0);
        }

        return new DrawnColor(
            Mix(Red, Alpha, below.Red, below.Alpha, alpha),
            Mix(Green, Alpha, below.Green, below.Alpha, alpha),
            Mix(Blue, Alpha, below.Blue, below.Alpha, alpha),
            alpha);
    }

    // WCAG's contrast ratio between two opaque colors.
    public static double Contrast(DrawnColor first, DrawnColor second)
    {
        var lighter = Math.Max(first.Luminance, second.Luminance);
        var darker = Math.Min(first.Luminance, second.Luminance);

        return (lighter + 0.05) / (darker + 0.05);
    }

    // The Euclidean distance in OKLab, times 100, between two opaque colors as seen in a color vision.
    public static double Distance(DrawnColor first, DrawnColor second, ColorVision vision)
    {
        var (firstLightness, firstA, firstB) = first.ToOklab(vision);
        var (secondLightness, secondA, secondB) = second.ToOklab(vision);

        return 100 * Math.Sqrt(Math.Pow(firstLightness - secondLightness, 2) + Math.Pow(firstA - secondA, 2) + Math.Pow(firstB - secondB, 2));
    }

    private static double Mix(double top, double topAlpha, double under, double underAlpha, double alpha)
        => ((top * topAlpha) + (under * underAlpha * (1 - topAlpha))) / alpha;

    private double Luminance
        => (0.2126 * Linear(Red)) + (0.7152 * Linear(Green)) + (0.0722 * Linear(Blue));

    private static double Linear(double channel)
        => channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);

    private (double Lightness, double A, double B) ToOklab(ColorVision vision)
    {
        var (red, green, blue) = (Linear(Red), Linear(Green), Linear(Blue));

        if (vision is not ColorVision.Full)
        {
            var simulation = vision is ColorVision.Protanopia ? Protanopia : Deuteranopia;
            (red, green, blue) = (
                Math.Clamp((simulation[0][0] * red) + (simulation[0][1] * green) + (simulation[0][2] * blue), 0, 1),
                Math.Clamp((simulation[1][0] * red) + (simulation[1][1] * green) + (simulation[1][2] * blue), 0, 1),
                Math.Clamp((simulation[2][0] * red) + (simulation[2][1] * green) + (simulation[2][2] * blue), 0, 1));
        }

        var l = Math.Cbrt((0.4122214708 * red) + (0.5363325363 * green) + (0.0514459929 * blue));
        var m = Math.Cbrt((0.2119034982 * red) + (0.6806995451 * green) + (0.1073969566 * blue));
        var s = Math.Cbrt((0.0883024619 * red) + (0.2817188376 * green) + (0.6299787005 * blue));

        return (
            (0.2104542553 * l) + (0.7936177850 * m) - (0.0040720468 * s),
            (1.9779984951 * l) - (2.4285922050 * m) + (0.4505937099 * s),
            (0.0259040371 * l) + (0.7827717662 * m) - (0.8086757660 * s));
    }
}
