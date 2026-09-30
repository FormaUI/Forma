namespace Forma.Framework.Files;

/// <summary>A file the picker declined, and why.</summary>
/// <param name="Name">The file's name.</param>
/// <param name="Reason">Why the picker declined the file.</param>
public sealed record RejectedFile(string Name, FileRejectionReason Reason);
