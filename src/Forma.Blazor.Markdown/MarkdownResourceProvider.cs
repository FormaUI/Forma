namespace Forma.Blazor.Markdown;

using System.Diagnostics.CodeAnalysis;

using Forma.Blazor.Components.Files;

using Ganss.Xss;

using Markdig;

/// <summary>Converts a Markdown document into sanitised markup, so a viewer can render it rather than offer it for download.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
public sealed class MarkdownResourceProvider : IResourceProvider
{
    private static readonly string[] Extensions = [".md", ".markdown"];

    // Raw HTML is dropped at the parser as well as at the sanitiser, so an embedded tag never reaches either.
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    private readonly IResourceContentReader _reader;
    private readonly HtmlSanitizer _sanitizer = CreateSanitizer();

    /// <summary>Creates the provider over the reader supplying document content.</summary>
    public MarkdownResourceProvider(IResourceContentReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        _reader = reader;
    }

    /// <summary>The extensions this provider claims, including the dot.</summary>
    public static IEnumerable<string> ClaimedExtensions
        => Extensions;

    /// <inheritdoc />
    public async ValueTask<ResourceInfo?> GetResourceAsync(string source, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        var resource = new ResourceInfo(source);

        if (!Claims(resource.Extension))
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
            using var buffer = new MemoryStream();

            await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            buffer.Position = 0;

            using var reader = new StreamReader(buffer, detectEncodingFromByteOrderMarks: true);

            var text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

            return new MarkupResourceInfo(source, _sanitizer.Sanitize(Markdown.ToHtml(text, Pipeline)))
            {
                Size = buffer.Length,
            };
        }
    }

    private static bool Claims(string extension)
        => Array.Exists(Extensions, claimed => string.Equals(extension, claimed, StringComparison.OrdinalIgnoreCase));

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        // Neither scheme can carry script, and a document routinely addresses a mailbox or a telephone.
        sanitizer.AllowedSchemes.Add("mailto");
        sanitizer.AllowedSchemes.Add("tel");

        return sanitizer;
    }
}
