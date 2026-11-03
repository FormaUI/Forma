namespace Forma.Framework.Culture.Browser.Interop;

using System.Diagnostics.CodeAnalysis;

/// <summary>What a runtime's registration starts the page's culture coordinator from on a page no server published a culture marker in, a standalone app's: the marker, the switch's policy and the document's direction.</summary>
[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)]
public sealed record CultureCoordinatorStart
{
    /// <summary>The culture marker's attributes, as <see cref="CulturePublication.GetMarkerAttributes"/> gives them.</summary>
    public required IReadOnlyDictionary<string, string> Marker { get; init; }

    /// <summary>The switch policy's attributes, as <see cref="CulturePublication.GetPolicyAttributes"/> gives them.</summary>
    public required IReadOnlyDictionary<string, string> Policy { get; init; }

    /// <summary>The direction the marker's UI culture writes in, <c>rtl</c> or <c>ltr</c>.</summary>
    public required string Direction { get; init; }
}
