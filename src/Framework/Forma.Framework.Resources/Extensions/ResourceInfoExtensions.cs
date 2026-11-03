namespace Forma.Framework.Resources.Extensions;

using System.Globalization;

/// <summary>Writes a resource's measurements for a reader.</summary>
public static class ResourceInfoExtensions
{
    extension(ResourceInfo resource)
    {
        /// <summary>Writes the size as a count of bytes in a culture.</summary>
        /// <param name="culture">The culture the number is written in.</param>
        /// <returns>The size written for a reader, or <see langword="null"/> where the size is unknown.</returns>
        public string? FormatSize(CultureInfo culture)
            => resource.Size is { } size ? ResourceFormatter.FromByteCount(size, culture) : null;
    }
}
