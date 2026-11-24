namespace Forma.Framework.Sanitization;

using System.Buffers;
using System.Text;

/// <summary>The schemes each kind of address allows: those that can't run script where the address is written.</summary>
/// <remarks>
/// An address is read as the browser reads it: its leading and trailing control characters and spaces are dropped, and every
/// tab and line break within it, and its scheme is what stands before the first colon where that is a letter followed by
/// letters, digits, <c>+</c>, <c>-</c> or <c>.</c>. An address without a scheme is relative, which takes the page's own.
/// A <c>data:</c> address is read up to its comma, the media type its bytes carry.
/// </remarks>
public static class AddressPolicy
{
    // The characters the browser drops from either end of an address: the C0 controls and the space.
    private const string ControlsAndSpace =
        "\0\u0001\u0002\u0003\u0004\u0005\u0006\u0007\b\t\n\u000b\f\r\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f ";

    // The whitespace a data address's media type may stand in.
    private const string AsciiWhitespace = "\t\n\f\r ";

    // The browser removes every tab and line break from an address before it reads one.
    private static readonly SearchValues<char> RemovedCharacters = SearchValues.Create("\t\n\r");

    // Raster images alone, since an SVG document can carry script.
    private static readonly string[] ImageMediaTypes =
    [
        "image/png", "image/apng", "image/jpeg", "image/jpg", "image/gif", "image/webp", "image/avif", "image/bmp",
        "image/x-icon", "image/vnd.microsoft.icon"
    ];

    /// <summary>Whether an address of the kind may be the given address.</summary>
    /// <param name="address">The address to judge.</param>
    /// <param name="kind">What the address is to the element written with it.</param>
    public static bool Allows(ReadOnlySpan<char> address, AddressKind kind)
    {
        var readAddress = Read(address);
        var scheme = GetSchemeSpan(readAddress);

        if (scheme.IsEmpty
            || scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
            || scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase) || scheme.Equals("tel", StringComparison.OrdinalIgnoreCase))
        {
            return kind is AddressKind.Link or AddressKind.DownloadLink;
        }

        if (scheme.Equals("blob", StringComparison.OrdinalIgnoreCase))
        {
            return kind is not AddressKind.Link;
        }

        return scheme.Equals("data", StringComparison.OrdinalIgnoreCase) && AllowsData(readAddress[(scheme.Length + 1)..], kind);
    }

    /// <summary>The scheme of an address as the browser reads it, in lowercase, or null for a relative address.</summary>
    /// <param name="address">The address whose scheme is read.</param>
    public static string? GetScheme(ReadOnlySpan<char> address)
    {
        var scheme = GetSchemeSpan(Read(address));

        if (scheme.IsEmpty)
        {
            return null;
        }

        // A scheme is ASCII by the way it is read.
        var loweredScheme = new char[scheme.Length];
        Ascii.ToLower(scheme, loweredScheme, out _);

        return new string(loweredScheme);
    }

    private static ReadOnlySpan<char> Read(ReadOnlySpan<char> address)
    {
        var trimmedAddress = address.Trim(ControlsAndSpace);

        if (!trimmedAddress.ContainsAny(RemovedCharacters))
        {
            return trimmedAddress;
        }

        var readAddress = new char[trimmedAddress.Length];
        var length = 0;

        foreach (var character in trimmedAddress)
        {
            if (!RemovedCharacters.Contains(character))
            {
                readAddress[length++] = character;
            }
        }

        return readAddress.AsSpan(0, length);
    }

    private static ReadOnlySpan<char> GetSchemeSpan(ReadOnlySpan<char> address)
    {
        if (address.IsEmpty || !char.IsAsciiLetter(address[0]))
        {
            return [];
        }

        for (var index = 1; index < address.Length; index++)
        {
            var character = address[index];

            if (character == ':')
            {
                return address[..index];
            }

            if (!char.IsAsciiLetterOrDigit(character) && character is not ('+' or '-' or '.'))
            {
                return [];
            }
        }

        return [];
    }

    private static bool AllowsData(ReadOnlySpan<char> data, AddressKind kind)
    {
        var comma = data.IndexOf(',');

        if (comma < 0)
        {
            return false;
        }

        var mediaType = data[..comma];
        var parameters = mediaType.IndexOf(';');
        mediaType = (parameters < 0 ? mediaType : mediaType[..parameters]).Trim(AsciiWhitespace);

        return kind switch
        {
            AddressKind.DownloadLink => true,
            AddressKind.ImageSource => IsImageMediaType(mediaType),
            AddressKind.VideoSource => IsOfType(mediaType, "video/"),
            AddressKind.AudioSource => IsOfType(mediaType, "audio/"),
            AddressKind.TrackSource => mediaType.Equals("text/vtt", StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }

    private static bool IsImageMediaType(ReadOnlySpan<char> mediaType)
    {
        foreach (var imageMediaType in ImageMediaTypes)
        {
            if (mediaType.Equals(imageMediaType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOfType(ReadOnlySpan<char> mediaType, string type)
        => mediaType.Length > type.Length && mediaType.StartsWith(type, StringComparison.OrdinalIgnoreCase);
}
