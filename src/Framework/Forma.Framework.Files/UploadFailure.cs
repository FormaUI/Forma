namespace Forma.Framework.Files;

/// <summary>A file whose upload failed, and the error that stopped it.</summary>
/// <param name="Name">The file's name.</param>
/// <param name="Error">The error that stopped the upload.</param>
public sealed record UploadFailure(string Name, Exception Error);
