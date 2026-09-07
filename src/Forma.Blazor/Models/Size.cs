namespace Forma.Blazor.Models;

/// <summary>A width and a height, in CSS pixels.</summary>
public readonly record struct Size
{
    /// <summary>Creates a size; neither side may be negative.</summary>
    public Size(double width, double height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);

        Width = width;
        Height = height;
    }

    /// <summary>The extent along the inline axis.</summary>
    public double Width { get; }

    /// <summary>The extent along the block axis.</summary>
    public double Height { get; }

    /// <summary>Reads a size from a width and height pair.</summary>
    public static implicit operator Size((double Width, double Height) size)
    {
        return new(size.Width, size.Height);
    }

    /// <summary>Reads a size from a width and height pair.</summary>
    public static Size FromTuple((double Width, double Height) size)
        => size;
}
