namespace Forma.Framework.Browser.Interop;

using System.Text.Json;

using Microsoft.JSInterop;

/// <summary>An owner's receiver of what its observations measure in the browser.</summary>
public sealed class DomReceiver
{
    private readonly DomSession _session;

    internal DomReceiver(DomSession session)
    {
        _session = session;
    }

    /// <summary>Delivers the state the observation holding <paramref name="token"/> measured under <paramref name="generation"/>.</summary>
    /// <returns>A task that completes as the observation takes the state.</returns>
    [JSInvokable]
    public Task NotifyAsync(string token, int generation, JsonElement state)
        => _session.DeliverAsync(token, generation, state);
}
