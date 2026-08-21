namespace Forma.UI.Features.Composer.Documents;

/// <summary>Document content together with the format it is written in.</summary>
public sealed record ComposerDocument(ComposerDocumentFormat Format, string Content);
