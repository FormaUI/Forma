namespace Forma.Framework.Catalog.Requests;

/// <summary>A catalog's rejection of an item.</summary>
/// <param name="Reason">The reason for the rejection.</param>
/// <param name="Allowed">The allowed ceiling that was reached.</param>
/// <param name="Message">The localized message describing the rejection.</param>
public sealed record CatalogRejection(CatalogRejectionReason Reason, long? Allowed, string Message);
