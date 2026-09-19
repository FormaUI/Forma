namespace Forma.Framework.Catalog.Requests;

/// <summary>Report that the catalog refused an item, and why.</summary>
/// <param name="Reason">Why the catalog refused.</param>
/// <param name="Allowed">The ceiling that was reached, or null when the reason is not a count.</param>
/// <param name="Message">The reason, written for a reader in their own language.</param>
public sealed record CatalogRejection(CatalogRejectionReason Reason, long? Allowed, string Message);
