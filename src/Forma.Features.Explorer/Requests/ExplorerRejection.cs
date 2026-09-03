namespace Forma.Features.Explorer.Requests;

/// <summary>Report that the explorer refused a resource, and why.</summary>
/// <param name="Reason">Why the explorer refused.</param>
/// <param name="Allowed">The ceiling that was reached, or null when the reason is not a count.</param>
/// <param name="Message">The reason, written for a reader in their own language.</param>
public sealed record ExplorerRejection(ExplorerRejectionReason Reason, long? Allowed, string Message);
