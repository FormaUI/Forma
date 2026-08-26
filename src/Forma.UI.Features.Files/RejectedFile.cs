namespace Forma.UI.Features.Files;

/// <summary>A file the picker declined, and why.</summary>
public sealed record RejectedFile(string Name, FileRejectionReason Reason);
