namespace Forma.Framework.Culture;

/// <summary>Handles culture routes identified by addresses.</summary>
public interface ICultureRouteHandler
{
    /// <summary>Handles the culture route represented by the supplied address.</summary>
    /// <param name="address">The address to handle.</param>
    /// <returns>A task that completes when culture route handling has finished.</returns>
    Task HandleAsync(Uri address);
}
