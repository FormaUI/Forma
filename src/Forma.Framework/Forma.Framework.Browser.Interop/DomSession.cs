namespace Forma.Framework.Browser.Interop;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

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

    /// <summary>Drops a registration released on its own.</summary>
    public void Remove(IDomRegistration registration)
    {
        _registrations.Remove(registration);
        _stateTargets.Remove(registration.Token);
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
    public Task DeliverAsync(string token, int generation, JsonElement state)
        => _stateTargets.TryGetValue(token, out var deliverAsync) ? deliverAsync(generation, state) : Task.CompletedTask;

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
                await TryReleaseAsync(root, "release");
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
            await TryReleaseAsync(DomRoot.Engine, "releaseEngine");
        }
    }

    private void ReleaseRegistrations(Func<IDomRegistration, bool> predicate)
    {
        foreach (var registration in _registrations.Where(predicate).ToList())
        {
            registration.Release();
            Remove(registration);
        }
    }

    private async Task TryReleaseAsync(DomRoot root, string identifier)
    {
        var module = await TryImportAsync(root, identifier);

        if (module is not null)
        {
            await TryInvokeCoreAsync<object>(module, root, identifier, identifier, [RuntimeKey]);
        }
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
