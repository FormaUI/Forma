namespace Forma.Blazor.Components.Interaction;

using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>An element registered with the input router as a scope, answering the routes its profile declares and held active while its surface is open.</summary>
public sealed partial class FormaInputScope : FormaElementBase
{
    private InputRegistration _registration = default!;

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

    /// <inheritdoc />
    protected override void OnInitialized()
        => _registration = new InputRegistration(
            InputRouter,
            (action, input) => OnAction.InvokeAsync(new InputAction(action, input)),
            (action, reason) => OnDismiss.InvokeAsync(new InputDismissal(action, reason)));

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Profile);

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
        => _registration.SyncAsync(RootElement, Profile, active: Active, parent: Parent, within: Within);

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-input-scope")
            .Add("forma-input-scope-active", Active);

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _registration.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
