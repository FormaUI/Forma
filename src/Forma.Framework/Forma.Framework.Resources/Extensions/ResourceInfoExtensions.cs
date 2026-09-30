namespace Forma.Framework.Resources.Extensions;

/// <summary>Writes a resource's measurements for a reader.</summary>
public static class ResourceInfoExtensions
{
    extension(ResourceInfo resource)
    {
        /// <summary>The size written for a reader as a count of bytes.</summary>
        public string? DisplaySize
            => resource.Size is { } size ? ResourceFormatter.FromByteCount(size) : null;
    }
}
