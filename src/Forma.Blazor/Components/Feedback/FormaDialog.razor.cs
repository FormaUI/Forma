namespace Forma.Blazor.Components.Feedback;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Common.Interop.Components;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A dialog rendered in the browser's top layer, with an optional header, footer actions, and scrim dismissal, registered with the input router as a modal scope while open so Escape and a platform close request are decided there.</summary>
public sealed partial class FormaDialog : IAsyncDisposable
{
    private static readonly InputProfile ModalProfile = new()
    {
        Modal = true,
        Escape = DismissBehavior.Dismiss,
        CloseRequest = DismissBehavior.Dismiss,
        PressAway = PressAwayBehavior.Ignore,
    };

    private ElementReference _dialogElement;
    private Dialog _dialog = default!;
    private DotNetObjectReference<FormaDialog> _selfReference = default!;
    private InputScope? _inputScope;
    private bool _isOpen;

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IStringLocale<FormaDialog> L { get; set; } = default!;
    [Inject] private InputRouter InputRouter { get; set; } = default!;

    /// <summary>Whether the dialog is currently open.</summary>
    [Parameter, EditorRequired] public bool Opened { get; set; }

    /// <summary>Fired when the dialog is closed (by Escape, scrim click, or close button).</summary>
    [Parameter, EditorRequired] public EventCallback OnClose { get; set; }

    /// <summary>Title shown in the header. If null, the header is hidden.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Size variant controlling the dialog width.</summary>
    [Parameter] public DialogSize Size { get; set; } = DialogSize.Medium;

    /// <summary>Default close and footer-action behavior.</summary>
    [Parameter] public DialogOptions Options { get; set; } = DialogOptions.Default;

    /// <summary>Main content of the dialog.</summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>Optional footer content (typically action buttons).</summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>Optional override text for the default confirm action.</summary>
    [Parameter] public string? ConfirmText { get; set; }

    /// <summary>Optional override text for the default cancel action.</summary>
    [Parameter] public string? CancelText { get; set; }

    /// <summary>Optional callback invoked when the default confirm action is clicked.</summary>
    [Parameter] public EventCallback OnConfirm { get; set; }

    /// <summary>Optional callback invoked when the default cancel action is clicked.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    private bool ShowHeader => Title is not null;
    private bool HasDefaultFooterActions => Options.ShowCancelAction || Options.ShowConfirmAction;
    private bool ShowFooter => Footer is not null || HasDefaultFooterActions;
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
    private string SizeClass => $"forma-dialog-{Size.ToString().ToLowerInvariant()}";
    private string ConfirmLabel => ConfirmText ?? L["Confirm"];
    private string CancelLabel =>
        CancelText ?? (Options.ShowCancelAction && !Options.ShowConfirmAction ? L["Close"] : L["Cancel"]);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _dialog = new Dialog(JS);
        _selfReference = DotNetObjectReference.Create(this);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Options);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _dialog.AttachAsync(_dialogElement);
        }

        if (Opened && !_isOpen)
        {
            // Content that renders again while the attach round-trip is still in flight brings a
            // second pass through here, and a call made before the module answered never ran. Only
            // a call that did run counts as open, so the pass that follows it opens the dialog.
            _isOpen = await _dialog.OpenAsync(Options.CloseOnScrimClick);

            if (_isOpen && _inputScope is null)
            {
                await RegisterAsync();
            }
        }
        else if (!Opened && _isOpen)
        {
            await SetClosedAsync();
            await _dialog.CloseAsync();
        }
    }

    /// <summary>Closes the dialog as the router dismissed it, or runs the default cancel action when told to cancel.</summary>
    [JSInvokable]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "The router hands every owner the action and the reason.")]
    public Task HandleDismissAsync(DismissBehavior action, DismissReason reason)
        => action switch
        {
            DismissBehavior.Dismiss => RequestCloseAsync(),
            DismissBehavior.Cancel => HandleDefaultCancelAsync(),
            _ => Task.CompletedTask,
        };

    /// <summary>
    /// Called when the native dialog fires its <c>close</c> event (Escape key or <c>.close()</c>).
    /// Notifies the parent to update <see cref="Opened"/>.
    /// </summary>
    private async Task HandleDialogClose()
    {
        if (_isOpen)
        {
            await SetClosedAsync();
            await OnClose.InvokeAsync();
        }
    }

    private async Task RequestCloseAsync()
    {
        if (await _dialog.CloseAsync())
        {
            return;
        }

        // The dialog's own close event cannot fire when the call never ran, so the state
        // is closed here instead.
        if (_isOpen)
        {
            await SetClosedAsync();
            await OnClose.InvokeAsync();
        }
    }

    private async Task HandleDefaultCancelAsync()
    {
        if (OnCancel.HasDelegate)
        {
            await OnCancel.InvokeAsync();
        }

        if (Options.CloseOnCancel)
        {
            await RequestCloseAsync();
        }
    }

    private async Task HandleDefaultConfirmAsync()
    {
        if (OnConfirm.HasDelegate)
        {
            await OnConfirm.InvokeAsync();
        }

        if (Options.CloseOnConfirm)
        {
            await RequestCloseAsync();
        }
    }

    // The dialog is a scope only while open, active the whole time so it stands as the modal barrier.
    private async Task RegisterAsync()
    {
        var inputScope = await InputRouter.RegisterAsync(_dialogElement, ModalProfile, _selfReference);

        if (inputScope is null)
        {
            return;
        }

        // The dialog may have closed, or another pass may have registered it, while the registration was being made.
        if (!_isOpen || _inputScope is not null)
        {
            await inputScope.DisposeAsync();
            return;
        }

        _inputScope = inputScope;
        await inputScope.ActivateAsync();
    }

    private async Task SetClosedAsync()
    {
        _isOpen = false;

        if (_inputScope is { } inputScope)
        {
            _inputScope = null;
            await inputScope.DisposeAsync();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await SetClosedAsync();
        await _dialog.DisposeAsync();
        _selfReference.Dispose();
    }
}
