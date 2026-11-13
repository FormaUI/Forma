namespace Forma.Framework.Files;

using Forma.Framework.Resources;

/// <summary>A file the store refused, and why.</summary>
/// <param name="Name">The file's name.</param>
/// <param name="Refusal">The store's refusal of the file.</param>
/// <param name="Message">The localized message describing the refusal.</param>
public sealed record UploadRefusal(string Name, FileRefusal Refusal, string Message);
