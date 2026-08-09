namespace Forma.Blazor.Office;

/// <summary>Turns one Office format into markup a viewer can render.</summary>
internal interface IOfficeConverter
{
    /// <summary>The file extension this converter reads, including the dot.</summary>
    string Extension { get; }

    /// <summary>The document's content as markup, which the caller sanitises before rendering.</summary>
    ValueTask<string> ConvertAsync(Stream content, CancellationToken cancellationToken);
}
