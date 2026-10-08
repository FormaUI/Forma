namespace Forma.Framework.Browser.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>What a registration is to the shared lifetime that settles it: how it is established in the browser, and what it does as a generation is established or invalidated.</summary>
/// <typeparam name="TOptions">The options the registration takes.</typeparam>
internal interface IDomRegistrationHost<TOptions>
    where TOptions : notnull
{
    /// <summary>Establishes the generation on <paramref name="element"/> with <paramref name="options"/> and reports whether the browser holds it.</summary>
    Task<bool> EstablishAsync(ElementReference element, TOptions options, int generation);

    /// <summary>Takes the generation as established, once it is the registration's current one.</summary>
    void OnEstablished(int generation);

    /// <summary>Drops whatever the generation that was current had established.</summary>
    void OnInvalidated();
}
