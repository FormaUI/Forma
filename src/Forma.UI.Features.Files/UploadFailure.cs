namespace Forma.UI.Features.Files;

/// <summary>A file whose upload did not complete, and what stopped it.</summary>
public sealed record UploadFailure(string Name, Exception Error);
