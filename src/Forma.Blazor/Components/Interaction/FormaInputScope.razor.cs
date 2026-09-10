namespace Forma.Blazor.Components.Interaction;

using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>An element registered with the input router as a scope, answering the routes its profile declares and held active while its surface is open.</summary>
public sealed partial class FormaInputScope : FormaElementBase
{
    private DotNetObjectReference<FormaInputScope> _selfReference = default!;
    private InputScope? _inputScope;
    private InputProfile? _registeredProfile;
    private Task _sync = Task.CompletedTask;

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

    [Inject] private InputRouter InputRouter { get; set; } = default!;

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
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-input-scope")
            .Add("forma-input-scope-active", Active);

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        try
        {
            if (_inputScope is { } inputScope)
            {
                _inputScope = null;
                await inputScope.DisposeAsync();
            }
        }
        finally
        {
            _selfReference.Dispose();
        }

        await base.DisposeAsyncCore();
    }

    private async Task SyncAsync(Task previous)
    {
        await previous;

        if (IsDisposed)
        {
            return;
        }

        if (_inputScope is null)
        {
            var inputScope = await InputRouter.RegisterAsync(RootElement, Profile, _selfReference, Parent, Within);

            // The component may have gone while the registration was being made; a scope on a gone element is released at once.
            if (IsDisposed)
            {
                if (inputScope is not null)
                {
                    await inputScope.DisposeAsync();
                }

                return;
            }

            _inputScope = inputScope;
            _registeredProfile = Profile;
        }

        if (_inputScope is null)
        {
            return;
        }

        if (!ReferenceEquals(_registeredProfile, Profile) && await _inputScope.UpdateAsync(Profile))
        {
            _registeredProfile = Profile;
        }

        if (Active && !_inputScope.IsActive)
        {
            await _inputScope.ActivateAsync();
        }
        else if (!Active && _inputScope.IsActive)
        {
            await _inputScope.ReleaseAsync();
        }
    }
}
