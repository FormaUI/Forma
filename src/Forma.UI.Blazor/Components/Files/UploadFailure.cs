namespace Forma.UI.Blazor.Components.Files;

/// <summary>A file whose upload did not complete, and what stopped it.</summary>
public sealed record UploadFailure(string Name, Exception Error);
