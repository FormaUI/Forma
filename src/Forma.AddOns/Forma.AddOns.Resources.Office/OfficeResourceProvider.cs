namespace Forma.Framework.Resources.Office;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Localization;
using Forma.Framework.Resources;
using Forma.Framework.Resources.Office.Converters;

using Ganss.Xss;

/// <summary>Converts a Word, Excel or PowerPoint document into sanitized markup a viewer can render.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
public sealed class OfficeResourceProvider : IResourceProvider
{
    private readonly Dictionary<string, IOfficeConverter> _convertersByExtension;
    private readonly IResourceContentReader _reader;
    private readonly HtmlSanitizer _sanitizer = CreateSanitizer();

    /// <summary>Creates the provider over the reader supplying document content.</summary>
    public OfficeResourceProvider(IResourceContentReader reader, IStringLocale<OfficeResourceProvider> locale)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(locale);

        _reader = reader;

        _convertersByExtension = new IOfficeConverter[]
        {
            new WordConverter(),
            new WorkbookConverter(locale),
            new PresentationConverter(locale),
        }.ToDictionary(converter => converter.Extension, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The extensions this provider claims, including the dot.</summary>
    public IEnumerable<string> ClaimedExtensions => _convertersByExtension.Keys;

    /// <inheritdoc />
    public async ValueTask<ResourceInfo?> GetResourceAsync(string source, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        var resource = new FileResourceInfo(source);

        if (!_convertersByExtension.TryGetValue(resource.Extension, out var converter))
        {
            return null;
        }

        var content = await _reader.OpenAsync(source, cancellationToken).ConfigureAwait(false);

        if (content is null)
        {
            return null;
        }

        await using (content.ConfigureAwait(false))
        {
            // Open XML reads the package by seeking.
            var buffered = content.CanSeek ? null : await BufferAsync(content, cancellationToken).ConfigureAwait(false);
            var seekable = buffered ?? content;

            try
            {
                var html = await converter.ConvertAsync(seekable, cancellationToken).ConfigureAwait(false);

                return new MarkupResourceInfo(source, _sanitizer.Sanitize(html))
                {
                    Size = seekable.Length,
                };
            }
            finally
            {
                if (buffered is not null)
                {
                    await buffered.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }

    private static async ValueTask<Stream> BufferAsync(Stream content, CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();

        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;

        return buffer;
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        sanitizer.AllowedSchemes.Add("data");

        // mailto and tel carry no script.
        sanitizer.AllowedSchemes.Add("mailto");
        sanitizer.AllowedSchemes.Add("tel");

        sanitizer.FilterUrl += RejectDataUrlOutsideAnImage;

        return sanitizer;
    }

    // A data URL is legitimate only as a converted image.
    private static void RejectDataUrlOutsideAnImage(object? sender, FilterUrlEventArgs args)
    {
        if (args.SanitizedUrl is not { } url || !url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var isImageSource = string.Equals(args.Tag?.TagName, "IMG", StringComparison.OrdinalIgnoreCase)
            && url.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);

        if (!isImageSource)
        {
            args.SanitizedUrl = null;
        }
    }
}
