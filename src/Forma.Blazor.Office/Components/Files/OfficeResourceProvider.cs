namespace Forma.Blazor.Components.Files;

using System.Diagnostics.CodeAnalysis;

using Forma.Blazor.Components.Files.Converters;

using Ganss.Xss;

using Microsoft.Extensions.Localization;

/// <summary>Converts a Word, Excel or PowerPoint document into sanitised markup, so a viewer can render one it could not otherwise show.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
public sealed class OfficeResourceProvider : IResourceProvider
{
    private readonly Dictionary<string, IOfficeConverter> _convertersByExtension;
    private readonly IResourceContentReader _reader;
    private readonly HtmlSanitizer _sanitizer = CreateSanitizer();

    /// <summary>Creates the provider over the reader supplying document content.</summary>
    public OfficeResourceProvider(IResourceContentReader reader, IStringLocalizer<OfficeResourceProvider> localizer)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(localizer);

        _reader = reader;

        // Every Office format is a ZIP underneath, so only the extension distinguishes them without opening the package.
        _convertersByExtension = new IOfficeConverter[]
        {
            new WordConverter(),
            new WorkbookConverter(localizer),
            new PresentationConverter(localizer),
        }.ToDictionary(converter => converter.Extension, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The extensions this provider claims, including the dot.</summary>
    public IEnumerable<string> ClaimedExtensions => _convertersByExtension.Keys;

    /// <inheritdoc />
    public async ValueTask<ResourceInfo?> GetResourceAsync(string source, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        var resource = new ResourceInfo(source);

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
            // Open XML reads the package by seeking, which a reader seeing a network response cannot offer.
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
        sanitizer.FilterUrl += RejectDataUrlOutsideAnImage;

        return sanitizer;
    }

    // Conversion embeds images as data URLs, which is the only place one is ever legitimate here.
    private static void RejectDataUrlOutsideAnImage(object? sender, FilterUrlEventArgs e)
    {
        if (e.SanitizedUrl is not { } url || !url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var isImageSource = string.Equals(e.Tag?.TagName, "IMG", StringComparison.OrdinalIgnoreCase)
            && url.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);

        if (!isImageSource)
        {
            e.SanitizedUrl = null;
        }
    }
}
