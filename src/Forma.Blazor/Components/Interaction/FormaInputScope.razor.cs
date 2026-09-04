namespace Forma.Blazor.Components.Interaction;

using Forma.Framework.Input;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>An element registered with the input router as a scope, answering the routes its profile declares and held active while its surface is open.</summary>
public sealed partial class FormaInputScope : ComponentBase, IAsyncDisposable
{
    private ElementReference _element;
    private DotNetObjectReference<FormaInputScope> _selfReference = default!;
    private InputRegistration? _registration;
    private InputProfile? _registeredProfile;
    private Task _sync = Task.CompletedTask;
    private bool _disposed;

    /// <summary>The routes the scope answers and how it treats ambient input while foremost.</summary>
    [Parameter] public InputProfile Profile { get; set; } = InputProfile.Default;

    /// <summary>Whether the scope is in the active ordering, receiving Escape, a press away, and close requests while foremost.</summary>
    [Parameter] public bool Active { get; set; }

    /// <summary>The scope this one stands inside for active ordering; <see langword="null"/> takes the nearest scope containing it.</summary>
    [Parameter] public ElementReference? Parent { get; set; }

    /// <summary>Elements outside the scope's element that count as inside it, such as the trigger that opened it.</summary>
    [Parameter] public IReadOnlyList<ElementReference> Within { get; set; } = [];

    /// <summary>The content the scope owns input within.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Raised with each action the router resolves to this scope.</summary>
    [Parameter] public EventCallback<InputAction> OnAction { get; set; }

    /// <summary>Raised when the router dismisses this scope while it is foremost.</summary>
    [Parameter] public EventCallback<InputDismissal> OnDismiss { get; set; }

    /// <summary>Additional attributes applied to the scope's element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private InputRouter Router { get; set; } = default!;

    /// <summary>The scope's element, for a sibling scope that declares it as parent.</summary>
    public ElementReference Element => _element;

    private Dictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = AdditionalAttributes is null
                ? []
                : new Dictionary<string, object>(AdditionalAttributes, StringComparer.Ordinal);

            attributes["class"] = AdditionalAttributes.MergeClass("forma-input-scope", Active ? "forma-input-scope-active" : null);

            return attributes;
        }
    }

    /// <summary>Performs an action the router resolved to this scope.</summary>
    [JSInvokable]
    public Task HandleInputActionAsync(string action, InputInfo input)
        => OnAction.InvokeAsync(new InputAction(action, input));

    /// <summary>Takes the dismissal the router delivered to this scope.</summary>
    [JSInvokable]
    public Task HandleDismissAsync(DismissBehavior action, DismissReason reason)
        => OnDismiss.InvokeAsync(new InputDismissal(action, reason));

    /// <inheritdoc />
    protected override void OnInitialized()
        => _selfReference = DotNetObjectReference.Create(this);

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Profile);

    // Renders can overlap, so each sync waits for the one before it and none registers twice.
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var previous = _sync;
        _sync = SyncAsync(previous);
        await _sync;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;

        try
        {
            if (_registration is { } registration)
            {
                _registration = null;
                await registration.DisposeAsync();
            }
        }
        finally
        {
            _selfReference.Dispose();
        }
    }

    private async Task SyncAsync(Task previous)
    {
        await previous;

        if (_disposed)
        {
            return;
        }

        if (_registration is null)
        {
            var registration = await Router.RegisterAsync(_element, Profile, _selfReference, Parent, Within);

            // The component may have gone while the registration was being made; a scope on a gone element is released at once.
            if (_disposed)
            {
                if (registration is not null)
                {
                    await registration.DisposeAsync();
                }

                return;
            }

            _registration = registration;
            _registeredProfile = Profile;
        }

        if (_registration is null)
        {
            return;
        }

        if (!ReferenceEquals(_registeredProfile, Profile) && await _registration.UpdateAsync(Profile))
        {
            _registeredProfile = Profile;
        }

        if (Active && !_registration.IsActive)
        {
            await _registration.ActivateAsync();
        }
        else if (!Active && _registration.IsActive)
        {
            await _registration.ReleaseAsync();
        }
    }
}
