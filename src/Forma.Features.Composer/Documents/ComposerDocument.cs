namespace Forma.Features.Composer.Documents;

using Forma.Framework.Resources;

/// <summary>Document content together with the format it is written in, and the files carried beside it.</summary>
public sealed record ComposerDocument(ComposerDocumentFormat Format, string Content)
{
    /// <summary>Files the document carries the way an email carries its attachments, held beside the content rather than written into it.</summary>
    public IReadOnlyList<ResourceInfo> Attachments { get; init; } = [];
}
