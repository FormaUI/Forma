namespace Forma.Framework.Browser.Interop;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Microsoft.AspNetCore.Components;

/// <summary>An observation of an element's state, kept in step with the target and options its owner last gave it, which hands each state the browser measures to its handler.</summary>
/// <typeparam name="TOptions">The options the observation takes.</typeparam>
/// <typeparam name="TState">The state the observation measures.</typeparam>
/// <remarks>
/// Establishing the observation returns its first state, which becomes <see cref="Current"/> and is handed to the handler before any state
/// measured later; a state arriving before the first is held until it has been taken, and only the latest so held is kept. The handler is
/// called once at a time, in the order the states were measured, and a handler that throws is logged under the owner and the registration.
/// A state that crosses while the handler is still busy waits in the browser, which keeps only the latest.
/// </remarks>
public sealed class DomObservation<TOptions, TState> : IAsyncDisposable, IDomRegistrationHost<TOptions>
    where TOptions : notnull
    where TState : class
{
    private readonly DomSession _session;
    private readonly Func<TState, Task> _handler;
    private readonly DomRegistration<TOptions> _registration;
    private EstablishmentBarrier? _barrier;
    private Task _delivery = Task.CompletedTask;

    internal DomObservation(DomSession session, DomCapability<TOptions> capability, Func<TState, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _session = session;
        _handler = handler;
        _registration = new DomRegistration<TOptions>(session, capability, this);
        session.AddStateTarget(_registration.Token, DeliverAsync);
    }

    /// <summary>The state the browser last measured, while the observation is established.</summary>
    public TState? Current { get; private set; }

    /// <summary>Whether the observation is established and its first state taken.</summary>
    public bool IsActive
        => _registration.IsActive;

    /// <summary>Whether the observation has been disposed.</summary>
    public bool IsDisposed
        => _registration.IsDisposed;

    /// <summary>Brings the observation in step with what its owner wants: <paramref name="element"/> observed with <paramref name="options"/> while <paramref name="shouldObserve"/>.</summary>
    /// <remarks>Concurrent calls join one settle, which completes once the latest request has been applied; a request equal to the one established makes no call.</remarks>
    public Task SyncAsync(ElementReference element, TOptions options, bool shouldObserve = true)
        => _registration.SyncAsync(element, options, shouldObserve);

    /// <summary>Measures the observation afresh and resumes its delivery.</summary>
    public async Task RefreshAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        if (!IsActive)
        {
            await _registration.RetryAsync();
            return;
        }

        await _registration.TryInvokeAsync<bool>("refresh", createsState: false, _session.RuntimeKey, _registration.Token, _registration.Generation);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _registration.DisposeAsync();

    /// <inheritdoc />
    async Task<bool> IDomRegistrationHost<TOptions>.EstablishAsync(ElementReference element, TOptions options, int generation)
    {
        var barrier = _barrier = new EstablishmentBarrier(generation);
        var capability = _registration.Capability;
        var baseline = await _registration.TryInvokeAsync<TState>("attach", createsState: true, _session.RuntimeKey, _session.Receiver, new
        {
            token = _registration.Token,
            generation,
            capability = capability.Name,
            target = element,
            options = capability.DescribeOptions(options),
        });

        if (baseline is null)
        {
            barrier.Cancel();

            if (ReferenceEquals(_barrier, barrier))
            {
                _barrier = null;
            }

            return false;
        }

        barrier.Baseline = baseline;

        return true;
    }

    /// <inheritdoc />
    void IDomRegistrationHost<TOptions>.OnEstablished(int generation)
    {
        if (_barrier is not { Baseline: { } baseline } barrier || barrier.Generation != generation)
        {
            return;
        }

        _barrier = null;
        Current = baseline;
        Enqueue(baseline, generation);
        barrier.Open();
    }

    /// <inheritdoc />
    void IDomRegistrationHost<TOptions>.OnInvalidated()
    {
        _barrier?.Cancel();
        _barrier = null;
        Current = null;
    }

    private async Task DeliverAsync(int generation, JsonElement payload)
    {
        if (!IsCurrent(generation))
        {
            return;
        }

        if (_barrier is { } barrier && barrier.Generation == generation && !await barrier.HoldAsync())
        {
            return;
        }

        // A state from a generation whose attach never succeeded has no first state to follow.
        if (!IsCurrent(generation) || !IsActive)
        {
            return;
        }

        var state = payload.Deserialize<TState>(JsonSerializerOptions.Web)
            ?? throw new JsonException($"The browser delivered no {typeof(TState).Name}.");

        Current = state;
        Enqueue(state, generation);
        await _delivery;
    }

    private void Enqueue(TState state, int generation)
        => _delivery = DeliverInTurnAsync(_delivery, state, generation);

    [SuppressMessage("Design", "CA1031", Justification = "A handler's failure is the owner's, logged under it, and leaves later states to be delivered.")]
    private async Task DeliverInTurnAsync(Task previous, TState state, int generation)
    {
        await previous;

        if (!IsCurrent(generation))
        {
            return;
        }

        try
        {
            await _handler(state);
        }
        catch (Exception ex)
        {
            DomLog.HandlerFailed(_session.Logger, ex, _registration.Capability.Name, _registration.Token, generation, _session.RuntimeId);
        }
    }

    private bool IsCurrent(int generation)
        => !IsDisposed && _registration.Generation == generation;

    /// <summary>Holds what the browser delivers for a generation until its attach result has been taken.</summary>
    private sealed class EstablishmentBarrier(int generation)
    {
        private TaskCompletionSource<bool>? _held;

        public int Generation { get; } = generation;

        public TState? Baseline { get; set; }

        /// <summary>Holds a delivery, dropping one held before it; completes with whether it is to be taken.</summary>
        public Task<bool> HoldAsync()
        {
            _held?.TrySetResult(false);
            _held = new TaskCompletionSource<bool>();

            return _held.Task;
        }

        public void Open()
            => _held?.TrySetResult(true);

        public void Cancel()
            => _held?.TrySetResult(false);
    }
}
