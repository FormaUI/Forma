namespace Forma.Framework.Browser.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>The lifetime an observation and an interaction share: what the owner last asked for, what the browser last established, and one settle bringing the second in step with the first.</summary>
/// <typeparam name="TOptions">The options the registration takes.</typeparam>
/// <remarks>
/// Each sync records the latest request and joins the settle underway, which applies requests until the one it applied last is the latest, so a
/// request superseded while another was being applied is never applied. A request equal to the one established makes no call; one that failed
/// is applied again by the next sync. Each attempt runs under a new generation, minted before its call, so a result or a state arriving for an
/// earlier generation is dropped, and the browser drops an attach for an earlier generation still loading. Disposal invalidates the current
/// generation at once and releases what the browser may hold, without waiting for a settle underway.
/// </remarks>
internal sealed class DomRegistration<TOptions> : IDomRegistration
    where TOptions : notnull
{
    private readonly DomSession _session;
    private readonly IDomRegistrationHost<TOptions> _host;
    private Request? _request;
    private Established? _established;
    private Task? _settle;
    private bool _mayHoldBrowserState;

    /// <summary>Creates a registration of <paramref name="capability"/> in <paramref name="session"/>, established through <paramref name="host"/>, released from the start where <paramref name="isReleased"/> or the session has been released.</summary>
    public DomRegistration(DomSession session, DomCapability<TOptions> capability, IDomRegistrationHost<TOptions> host, bool isReleased = false)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(capability);
        ArgumentNullException.ThrowIfNull(host);

        _session = session;
        _host = host;
        Capability = capability;
        Token = session.CreateToken();
        IsDisposed = isReleased || !session.Add(this);
    }

    /// <summary>The capability the registration is of.</summary>
    public DomCapability<TOptions> Capability { get; }

    /// <inheritdoc />
    public string Token { get; }

    /// <inheritdoc />
    public DomRoot Root
        => Capability.Root;

    /// <summary>The generation the registration is under, minted afresh for each attempt to establish it and for its release.</summary>
    public int Generation { get; private set; }

    /// <summary>Whether the browser holds the registration as last requested.</summary>
    public bool IsActive
        => _established is not null;

    /// <summary>Whether the registration has been released.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>Records what the owner wants, <paramref name="element"/> registered with <paramref name="options"/> while <paramref name="shouldApply"/>, and joins the settle that brings the browser in step with it.</summary>
    public Task SyncAsync(ElementReference element, TOptions options, bool shouldApply)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (IsDisposed)
        {
            return Task.CompletedTask;
        }

        _request = new Request(element, options, shouldApply);

        return _settle is { IsCompleted: false } settle ? settle : _settle = SettleAsync();
    }

    /// <summary>Settles the latest request again, establishing it afresh where the browser does not hold it.</summary>
    public Task RetryAsync()
        => _request is { } request ? SyncAsync(request.Element, request.Options, request.ShouldApply) : Task.CompletedTask;

    /// <summary>Invokes <paramref name="identifier"/> on the registration's entry under the current generation, where <see langword="default"/> means the call did not run.</summary>
    public Task<TResult?> TryInvokeAsync<TResult>(string identifier, bool createsState, params object?[] args)
    {
        var generation = Generation;

        return _session.TryInvokeAsync<TResult>(Root, identifier, FormatOperation(identifier, generation), () => !IsDisposed && Generation == generation, createsState, args);
    }

    /// <inheritdoc />
    public void Release()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        Invalidate();
    }

    /// <summary>Releases the registration, and what the browser may hold for it.</summary>
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        var mayHoldBrowserState = _mayHoldBrowserState;
        Release();
        _session.Remove(this);

        if (mayHoldBrowserState)
        {
            await DetachAsync(Generation);
        }
    }

    private async Task SettleAsync()
    {
        while (!IsDisposed && _request is { } request)
        {
            // An unrendered @ref arrives as a reference with no id, which nothing can be registered on.
            if (!request.ShouldApply || request.Element.Id is not { Length: > 0 })
            {
                if (_mayHoldBrowserState || _established is not null)
                {
                    await RemoveAsync();
                }
            }
            else if (_established is not { } established || established.ElementId != request.Element.Id || !established.Options.Equals(request.Options))
            {
                await EstablishAsync(request);
            }

            if (ReferenceEquals(_request, request))
            {
                return;
            }
        }
    }

    private async Task EstablishAsync(Request request)
    {
        var generation = Invalidate();
        _mayHoldBrowserState = true;

        var isEstablished = await _host.EstablishAsync(request.Element, request.Options, generation);

        if (isEstablished && !IsDisposed && Generation == generation)
        {
            _established = new Established(request.Element.Id, request.Options);
            _host.OnEstablished(generation);
        }
    }

    private async Task RemoveAsync()
    {
        var generation = Invalidate();
        _mayHoldBrowserState = false;

        await DetachAsync(generation);
    }

    private async Task DetachAsync(int generation)
        => await _session.TryInvokeAsync<object>(Root, "detach", FormatOperation("detach", generation), static () => true, createsState: false, _session.RuntimeKey, Token, generation);

    // A new generation drops whatever the current one had established, and any result or state still arriving for it.
    private int Invalidate()
    {
        Generation++;
        _established = null;
        _host.OnInvalidated();

        return Generation;
    }

    private string FormatOperation(string identifier, int generation)
        => $"{identifier} {Capability.Name} {Token}.{generation}";

    /// <summary>What the owner asked for last.</summary>
    private sealed record Request(ElementReference Element, TOptions Options, bool ShouldApply);

    /// <summary>What the browser holds: the element, by its id, and the options it was established with.</summary>
    private sealed record Established(string ElementId, TOptions Options);
}
