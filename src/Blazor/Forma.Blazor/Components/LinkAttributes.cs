namespace Forma.Blazor.Components;

using Forma.Framework.Sanitization;

/// <summary>What the attributes a link is drawn with make of its address.</summary>
internal static class LinkAttributes
{
    /// <summary>The kind of address a link drawn with the attributes takes: a download link where they carry <c>download</c>.</summary>
    /// <param name="attributes">The attributes the link is drawn with.</param>
    public static AddressKind GetAddressKind(IReadOnlyDictionary<string, object>? attributes)
        => attributes?.Any(attribute => attribute.Key.Equals("download", StringComparison.OrdinalIgnoreCase) && attribute.Value is not false) is true
            ? AddressKind.DownloadLink
            : AddressKind.Link;
}
