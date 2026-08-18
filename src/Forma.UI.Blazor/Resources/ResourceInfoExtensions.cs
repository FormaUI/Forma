namespace Forma.UI.Blazor.Resources;

using Forma.Framework.Resources;

/// <summary>Writes a resource's measurements for a reader, which the resource itself does not carry.</summary>
internal static class ResourceInfoExtensions
{
    extension(ResourceInfo resource)
    {
        /// <summary>The size written for a reader as a count of bytes.</summary>
        public string? DisplaySize
            => resource.Size is { } size ? ResourceFormatter.FromByteCount(size) : null;
    }
}
