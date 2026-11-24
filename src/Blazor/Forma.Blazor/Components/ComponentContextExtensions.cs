namespace Forma.Blazor.Components;

using Forma.Framework.Sanitization;

/// <summary>What a component writes into a page through its context.</summary>
public static class ComponentContextExtensions
{
    /// <summary>The address a component writes in place of an address of the kind.</summary>
    /// <remarks>
    /// An address its kind allows is written as it is, and one its kind doesn't is written as none, so the element keeps its
    /// text and takes no address; the component's log records the refusal at <c>Debug</c>, naming the address's scheme.
    /// </remarks>
    /// <param name="context">The context of the component writing the address.</param>
    /// <param name="address">The address the component was given.</param>
    /// <param name="kind">What the address is to the element written with it.</param>
    public static string? GetAllowedAddress(this IComponentContext context, string? address, AddressKind kind)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (address is null || AddressPolicy.Allows(address, kind))
        {
            return address;
        }

        AddressLog.AddressWrittenInert(context.Logger, AddressPolicy.GetScheme(address), kind, context.RuntimeId);

        return null;
    }
}
