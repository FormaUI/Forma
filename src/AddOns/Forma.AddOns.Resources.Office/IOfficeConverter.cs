namespace Forma.Framework.Resources.Office;

/// <summary>A converter from one Office format into markup.</summary>
internal interface IOfficeConverter
{
    /// <summary>The file extension this converter reads, including the dot.</summary>
    string Extension { get; }

    /// <summary>Converts the document's content into raw markup, ahead of sanitizing.</summary>
    ValueTask<string> ConvertAsync(Stream content, CancellationToken cancellationToken);
}
