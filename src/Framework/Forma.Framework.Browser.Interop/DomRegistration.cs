namespace Forma.Framework.Browser.Interop;

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components;

/// <summary>A registration of a capability, kept in step with the element and options its owner last asked for.</summary>
/// <typeparam name="TOptions">The options the registration takes.</typeparam>
/// <remarks>
/// Each sync records the latest request and joins the settle underway, which applies requests until the one it applied last is the latest; a
/// request superseded while another was being applied is never applied. Each attempt runs under a generation minted before its call; a result
/// or a state arriving for an earlier generation is dropped, and the browser drops an attach for an earlier generation still loading.
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

    /// <summary>Creates a registration of a capability in a session.</summary>
    /// <param name="session">The session the registration is in.</param>
    /// <param name="capability">The capability the registration is of.</param>
    /// <param name="host">The host the registration is established through.</param>
    /// <param name="isReleased">Whether the registration starts released.</param>
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

    /// <inheritdoc />
    public int Generation { get; private set; }

    /// <inheritdoc />
    public bool IsActive
        => _established is not null;

    /// <summary>Whether the registration has been released.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>Brings the registration in step with what its owner wants: <paramref name="element"/> registered with <paramref name="options"/> while <paramref name="shouldApply"/>.</summary>
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

    /// <summary>Settles the owner's latest request again.</summary>
    public Task RetryAsync()
        => _request is { } request ? SyncAsync(request.Element, request.Options, request.ShouldApply) : Task.CompletedTask;

    /// <summary>Invokes <paramref name="identifier"/> on the registration's entry under its current generation and returns its result, where <see langword="default"/> means the call did not run.</summary>
    public Task<TResult?> TryInvokeAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TResult>(string identifier, bool createsState, params object?[] args)
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

    /// <summary>Releases the registration and what the browser holds for it.</summary>
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
            // An unrendered @ref arrives with no id.
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

    private int Invalidate()
    {
        Generation++;
        _established = null;
        _host.OnInvalidated();

        return Generation;
    }

    private string FormatOperation(string identifier, int generation)
        => $"{identifier} {Capability.Name} {Token}.{generation}";

    /// <summary>An owner's request: the element, its options, and whether they apply.</summary>
    private sealed record Request(ElementReference Element, TOptions Options, bool ShouldApply);

    /// <summary>What the browser holds: the element, by its id, and the options it was established with.</summary>
    private sealed record Established(string ElementId, TOptions Options);
}
