namespace Forma.Framework.Culture.Blazor.WebAssembly;

using System.Globalization;

/// <summary>Applies each accepted pair to a WebAssembly runtime's culture state and, the runtime being one user scope in one process, to the process's default cultures too.</summary>
/// <remarks>Work the runtime starts on a flow of its own, an event's or a timer's, takes the process's defaults, so it runs in the accepted pair whatever flow applied it.</remarks>
/// <param name="cultureState">The runtime's culture state, which is its own integration.</param>
internal sealed class ProcessCultureIntegration(ICultureState cultureState) : ICultureStateIntegration
{
    private readonly ICultureStateIntegration _integration = cultureState as ICultureStateIntegration
        ?? throw new InvalidOperationException($"The registered {nameof(ICultureState)}, {cultureState.GetType().Name}, doesn't implement {nameof(ICultureStateIntegration)}.");

    /// <inheritdoc />
    public Task ApplyAsync(AcceptedCulture acceptedCulture)
    {
        var notification = _integration.ApplyAsync(acceptedCulture);

        CultureInfo.DefaultThreadCurrentCulture = cultureState.Culture;
        CultureInfo.DefaultThreadCurrentUICulture = cultureState.UICulture;

        return notification;
    }
}
