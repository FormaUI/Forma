namespace Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>An input registration a component holds on one of its elements, in step with the profile and the hold the component last gave it.</summary>
/// <remarks>Each sync records what the component wants; one settle runs at a time and carries on until what is registered matches the latest sync, so a profile or an element that changed while a registration was in flight is applied once it lands, and a registration that lands unwanted is released at once. The parent and the elements counted as inside are given only when a registration is made, so a change to either releases the registration and makes it again. A registration the router refused is asked for again on the next sync. Disposal lets a settle underway finish before it releases what the settle registered and the reference the router calls back through goes, without waiting for it, so a runtime that never answers the settle, as a web view being torn down answers none, can't hold disposal open.</remarks>
public sealed class InputRegistration : IAsyncDisposable
{
    private readonly InputRouter _router;
    private readonly Func<string, InputInfo, Task>? _onAction;
    private readonly Func<DismissBehavior, DismissReason, Task>? _onDismiss;
    private readonly DotNetObjectReference<InputRegistration> _selfReference;
    private InputScope? _scope;
    private InputProfile? _registeredProfile;
    private string? _registeredElementId;
    private Placement? _registeredPlacement;
    private Request? _request;
    private Task? _settle;
    private bool _isDisposed;

    /// <summary>Creates a registration made through <paramref name="router"/>, performing actions through <paramref name="onAction"/> and dismissals through <paramref name="onDismiss"/>.</summary>
    public InputRegistration(InputRouter router, Func<string, InputInfo, Task>? onAction = null, Func<DismissBehavior, DismissReason, Task>? onDismiss = null)
    {
        ArgumentNullException.ThrowIfNull(router);

        _router = router;
        _onAction = onAction;
        _onDismiss = onDismiss;
        _selfReference = DotNetObjectReference.Create(this);
    }

    /// <summary>Whether the element is registered.</summary>
    public bool IsRegistered
        => _scope is not null;

    /// <summary>Whether the registration is held active.</summary>
    public bool IsActive
        => _scope is { IsActive: true };

    /// <summary>Brings the registration in step with what its owner wants: <paramref name="element"/> registered with <paramref name="profile"/> while <paramref name="wanted"/>, inside <paramref name="parent"/> with the <paramref name="within"/> elements counted as inside it, and held active while <paramref name="active"/>.</summary>
    public async Task SyncAsync(ElementReference element, InputProfile profile, bool wanted = true, bool active = false, ElementReference? parent = null, IReadOnlyList<ElementReference>? within = null)
    {
        ArgumentNullException.ThrowIfNull(profile);

        _request = new Request(element, profile, wanted && !_isDisposed, active, parent, within);

        if (_settle is { IsCompleted: false })
        {
            return;
        }

        _settle = SettleAsync();
        await _settle;
    }

    /// <summary>Performs an action the router resolved to the element.</summary>
    [JSInvokable]
    public Task HandleInputActionAsync(string action, InputInfo input)
        => _onAction?.Invoke(action, input) ?? Task.CompletedTask;

    /// <summary>Performs a dismissal the router delivered to the element.</summary>
    [JSInvokable]
    public Task HandleDismissAsync(DismissBehavior action, DismissReason reason)
        => _onDismiss?.Invoke(action, reason) ?? Task.CompletedTask;

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return ValueTask.CompletedTask;
        }

        _isDisposed = true;

        if (_request is { } request)
        {
            _request = request with { Wanted = false };
        }

        var settle = _settle;
        var ending = EndAsync(settle);

        return settle is { IsCompleted: false } ? ValueTask.CompletedTask : new ValueTask(ending);
    }

    private async Task SettleAsync()
    {
        while (_request is { } request)
        {
            // An unrendered @ref arrives as a reference with no id, which nothing can be registered on.
            if (!request.Wanted || request.Element.Id is not { Length: > 0 })
            {
                await ReleaseAsync();
            }
            else if (_scope is { } scope && _registeredElementId == request.Element.Id && _registeredPlacement == Placement.Of(request))
            {
                await KeepInStepAsync(scope, request);
            }
            else
            {
                await ReleaseAsync();
                await RegisterAsync(request);
            }

            if (ReferenceEquals(_request, request))
            {
                return;
            }
        }
    }

    private async Task RegisterAsync(Request request)
    {
        var scope = await _router.RegisterAsync(request.Element, request.Profile, _selfReference, request.Parent, request.Within);

        if (scope is null)
        {
            return;
        }

        _scope = scope;
        _registeredProfile = request.Profile;
        _registeredElementId = request.Element.Id;
        _registeredPlacement = Placement.Of(request);

        if (request.Active)
        {
            await scope.ActivateAsync();
        }
    }

    private async Task KeepInStepAsync(InputScope scope, Request request)
    {
        if (!ReferenceEquals(_registeredProfile, request.Profile) && await scope.UpdateAsync(request.Profile))
        {
            _registeredProfile = request.Profile;
        }

        if (request.Active && !scope.IsActive)
        {
            await scope.ActivateAsync();
        }
        else if (!request.Active && scope.IsActive)
        {
            await scope.ReleaseAsync();
        }
    }

    // A settle underway finishes before the registration it made is released and the reference the router calls back through goes.
    private async Task EndAsync(Task? settle)
    {
        try
        {
            if (settle is not null)
            {
                await Task.WhenAny(settle);
            }

            await ReleaseAsync();
        }
        finally
        {
            _selfReference.Dispose();
        }
    }

    private async Task ReleaseAsync()
    {
        _registeredProfile = null;
        _registeredElementId = null;
        _registeredPlacement = null;

        if (_scope is { } scope)
        {
            _scope = null;
            await scope.DisposeAsync();
        }
    }

    /// <summary>What the owner asked for last.</summary>
    private sealed record Request(ElementReference Element, InputProfile Profile, bool Wanted, bool Active, ElementReference? Parent, IReadOnlyList<ElementReference>? Within);

    /// <summary>A registration's parent and the elements counted as inside it, by their ids.</summary>
    private sealed record Placement(string? ParentId, string WithinIds)
    {
        public static Placement Of(Request request)
            => new(request.Parent?.Id, string.Join('\n', (request.Within ?? []).Select(element => element.Id)));
    }
}
