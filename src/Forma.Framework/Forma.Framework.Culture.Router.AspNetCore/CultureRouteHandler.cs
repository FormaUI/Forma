namespace Forma.Framework.Culture.Router.AspNetCore;

/// <summary>Handles a culture route by changing a user scope's culture state to the culture its address names.</summary>
internal sealed class CultureRouteHandler(CultureSwitcher cultureSwitcher) : ICultureRouteHandler
{
    /// <inheritdoc />
    public Task HandleAsync(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (!address.IsAbsoluteUri)
        {
            throw new InvalidOperationException("A culture route's address is absolute.");
        }

        return cultureSwitcher.ReconcileAsync(address);
    }
}
