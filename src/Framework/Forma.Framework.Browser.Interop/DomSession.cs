namespace Forma.Framework.Browser.Interop;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

using Forma.Framework.Browser.Interop.Text;
using Forma.Framework.JavaScript.Interop;

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>An owner's side of the browser: its registrations, its receiver, and the calls made for it, under its own log and identity.</summary>
internal sealed class DomSession
{
    private readonly IJSEnvironment _js;
    private readonly DomBridge? _sharedBridge;
    private readonly List<IDomRegistration> _registrations = [];
    private readonly Dictionary<string, Func<int, JsonElement, Task>> _stateTargets = [];
    private readonly Dictionary<string, DomTextEdits> _editTargets = [];
    private readonly HashSet<DomRoot> _reachedRoots = [];
    private DomBridge? _ownBridge;
    private DotNetObjectReference<DomReceiver>? _receiver;
    private int _lastToken;

    /// <summary>Creates the session of the owner whose environment is <paramref name="js"/>, importing through <paramref name="bridge"/>.</summary>
    public DomSession(IJSEnvironment js, DomBridge? bridge)
    {
        ArgumentNullException.ThrowIfNull(js);

        _js = js;
        _sharedBridge = bridge;
    }

    /// <summary>The text form of <see cref="RuntimeId"/>.</summary>
    public string RuntimeKey
        => _js.RuntimeId.ToString();

    /// <summary>The owner's runtime identity.</summary>
    public Guid RuntimeId
        => _js.RuntimeId;

    /// <summary>The owner's logger.</summary>
    public ILogger Logger
        => _js.Logger;

    /// <summary>What a value the browser sends the owner accepts where its receiver names no maximum of its own.</summary>
    public JSTransferOptions TransferOptions
        => _js.TransferOptions;

    /// <summary>Whether the session has been released.</summary>
    public bool IsReleased { get; private set; }

    /// <summary>The bridge the owner imports through.</summary>
    public DomBridge Bridge
        => _sharedBridge ?? (_ownBridge ??= new DomBridge(_js, inputRouter: null));

    /// <summary>The reference to the owner's receiver.</summary>
    public DotNetObjectReference<DomReceiver> Receiver
        => _receiver ??= DotNetObjectReference.Create(new DomReceiver(this));

    /// <summary>Mints a token for a new registration, unique within the owner.</summary>
    public string CreateToken()
        => (++_lastToken).ToString(CultureInfo.InvariantCulture);

    /// <summary>Takes <paramref name="registration"/> into the session, returning whether the session took it.</summary>
    public bool Add(IDomRegistration registration)
    {
        if (IsReleased)
        {
            return false;
        }

        _registrations.Add(registration);

        return true;
    }

    /// <summary>Routes the state the browser delivers under <paramref name="token"/> to <paramref name="deliverAsync"/>.</summary>
    public void AddStateTarget(string token, Func<int, JsonElement, Task> deliverAsync)
        => _stateTargets[token] = deliverAsync;

    /// <summary>Routes the edits the browser delivers under <paramref name="token"/> to <paramref name="edits"/>.</summary>
    public void AddEditTarget(string token, DomTextEdits edits)
        => _editTargets[token] = edits;

    /// <summary>Gets the text field's edits the browser delivers under <paramref name="token"/>.</summary>
    public bool TryGetEditTarget(string token, [NotNullWhen(true)] out DomTextEdits? edits)
        => _editTargets.TryGetValue(token, out edits);

    /// <summary>Starts bringing every text field's edits back in step with its field, as the connection comes back.</summary>
    public void BeginResync()
    {
        foreach (var edits in _editTargets.Values.ToList())
        {
            _ = edits.ResyncAsync();
        }
    }

    /// <summary>Drops a registration released on its own.</summary>
    public void Remove(IDomRegistration registration)
    {
        _registrations.Remove(registration);
        _stateTargets.Remove(registration.Token);
        _editTargets.Remove(registration.Token);
    }

    /// <summary>The owner's established observations, each by its token and current generation.</summary>
    public DomObservedOwner? DescribeObservations()
    {
        var observations = _registrations
            .Where(registration => registration.Root == DomRoot.Watcher && registration.IsActive)
            .Select(registration => new DomObservedRegistration { Token = registration.Token, Generation = registration.Generation })
            .ToList();

        return observations.Count == 0 ? null : new DomObservedOwner { RuntimeKey = RuntimeKey, Registrations = observations };
    }

    /// <summary>Delivers a state the browser measured to the registration holding <paramref name="token"/>.</summary>
    public async Task DeliverAsync(string token, int generation, JSTransfer state)
    {
        if (!_stateTargets.TryGetValue(token, out var deliverAsync))
        {
            await state.DiscardAsync();
            return;
        }

        JsonElement value;

        try
        {
            value = await state.ReadJsonAsync(_js.TransferOptions.MaximumLength);
        }
        catch (JSTransferException ex)
        {
            DomLog.StateLost(_js.Logger, ex, token, generation, _js.RuntimeId);
            return;
        }

        await deliverAsync(generation, value);
    }

    /// <summary>Invokes <paramref name="identifier"/> on <paramref name="root"/> and returns its result, where <see langword="default"/> means the call did not run.</summary>
    /// <param name="root">The entry the call is made through.</param>
    /// <param name="identifier">The entry's export.</param>
    /// <param name="operation">What the call does, as diagnostics name it.</param>
    /// <param name="isWanted">Whether the call is still wanted once the entry has been imported.</param>
    /// <param name="createsState">Whether the call can leave state in the browser that the owner's release has to reach.</param>
    /// <param name="args">The export's arguments.</param>
    public async Task<TResult?> TryInvokeAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TResult>(DomRoot root, string identifier, string operation, Func<bool> isWanted, bool createsState, params object?[] args)
    {
        var module = await TryImportAsync(root, operation);

        if (module is null || IsReleased || !isWanted())
        {
            return default;
        }

        if (createsState)
        {
            _reachedRoots.Add(root);
            _sharedBridge?.Track(this);
        }

        return await TryInvokeCoreAsync<TResult>(module, root, identifier, operation, args);
    }

    /// <summary>Releases every registration, then what the browser holds for the owner, in one call.</summary>
    public async ValueTask ReleaseAsync()
    {
        if (IsReleased)
        {
            return;
        }

        IsReleased = true;
        _sharedBridge?.Untrack(this);
        ReleaseRegistrations(_ => true);

        try
        {
            if (_reachedRoots.Count > 0)
            {
                var root = _reachedRoots.Contains(DomRoot.Engine) ? DomRoot.Engine : DomRoot.Watcher;
                await TryReleaseAsync(root, "release", "release", [RuntimeKey]);
            }
        }
        finally
        {
            _reachedRoots.Clear();
            _receiver?.Dispose();

            if (_ownBridge is not null)
            {
                await _ownBridge.DisposeAsync();
            }
        }
    }

    /// <summary>Releases the registrations made through the engine, then what the browser holds for the owner's engine, in one call.</summary>
    public async ValueTask ReleaseEngineAsync()
    {
        if (IsReleased)
        {
            return;
        }

        ReleaseRegistrations(registration => registration.Root == DomRoot.Engine);

        if (_reachedRoots.Remove(DomRoot.Engine))
        {
            await TryReleaseAsync(DomRoot.Engine, "releaseEngine", "releaseEngine", [RuntimeKey]);
        }
    }

    /// <summary>Detaches a registration from the browser through <paramref name="root"/>'s entry.</summary>
    /// <remarks>
    /// A detach, as a release, goes through the entry only once its import has landed: what the browser holds through a root was reached through
    /// a landed import, and a call waiting on an import still under way checks once it lands whether it is still wanted. A runtime that never
    /// answers an import, as a web view being torn down answers none, can't hold the detach open.
    /// </remarks>
    /// <param name="root">The entry the registration was made through.</param>
    /// <param name="operation">What the call does, as diagnostics name it.</param>
    /// <param name="args">The detach's arguments.</param>
    public Task TryDetachAsync(DomRoot root, string operation, params object?[] args)
        => IsReleased ? Task.CompletedTask : TryReleaseAsync(root, "detach", operation, args);

    private void ReleaseRegistrations(Func<IDomRegistration, bool> predicate)
    {
        foreach (var registration in _registrations.Where(predicate).ToList())
        {
            registration.Release();
            Remove(registration);
        }
    }

    // Reaches the browser only through an import that has landed, as everything the browser holds through the root did.
    private async Task TryReleaseAsync(DomRoot root, string identifier, string operation, object?[] args)
    {
        IJSObjectReference? module;

        try
        {
            if (!Bridge.TryGetImported(root, out module))
            {
                return;
            }
        }
        catch (ObjectDisposedException)
        {
            DomLog.BridgeReleased(_js.Logger, operation, root.Name, _js.RuntimeId);
            return;
        }

        await TryInvokeCoreAsync<object>(module, root, identifier, operation, args);
    }

    private async Task<IJSObjectReference?> TryImportAsync(DomRoot root, string operation)
    {
        try
        {
            return await Bridge.ImportAsync(root);
        }
        catch (JSDisconnectedException)
        {
            DomLog.CircuitDropped(_js.Logger, operation, root.Name, _js.RuntimeId);
        }
        catch (OperationCanceledException)
        {
            DomLog.CallCanceled(_js.Logger, operation, root.Name, _js.RuntimeId);
        }
        catch (JSException ex)
        {
            DomLog.ImportFailed(_js.Logger, ex, root.Name, operation, _js.RuntimeId);
        }
        catch (ObjectDisposedException)
        {
            DomLog.BridgeReleased(_js.Logger, operation, root.Name, _js.RuntimeId);
        }

        return null;
    }

    private async Task<TResult?> TryInvokeCoreAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TResult>(IJSObjectReference module, DomRoot root, string identifier, string operation, object?[] args)
    {
        try
        {
            return await module.InvokeAsync<TResult>(identifier, args);
        }
        catch (JSDisconnectedException)
        {
            DomLog.CircuitDropped(_js.Logger, operation, root.Name, _js.RuntimeId);
        }
        catch (OperationCanceledException)
        {
            DomLog.CallCanceled(_js.Logger, operation, root.Name, _js.RuntimeId);
        }
        catch (JSException ex)
        {
            DomLog.CallFailed(_js.Logger, ex, operation, root.Name, _js.RuntimeId);
        }
        catch (ObjectDisposedException)
        {
            DomLog.BridgeReleased(_js.Logger, operation, root.Name, _js.RuntimeId);
        }

        return default;
    }
}
