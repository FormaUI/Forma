namespace Forma.UI.Components.Files;

/// <summary>A file <see cref="FormaFilePicker"/> declined, and why.</summary>
public sealed record RejectedFile(string Name, FileRejectionReason Reason);
