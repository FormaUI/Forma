using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

namespace Forma.Blazor.Components.Admin.Feedback;

public sealed partial class FormaModal : IAsyncDisposable
{
    private ElementReference _dialogRef;
    private IJSObjectReference? _module;
    private bool _isOpen;

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IStringLocalizer<FormaModal> L { get; set; } = default!;

    /// <summary>Whether the modal is currently open.</summary>
    [Parameter, EditorRequired] public bool Opened { get; set; }

    /// <summary>Fired when the modal is closed (by Escape, backdrop click, or close button).</summary>
    [Parameter, EditorRequired] public EventCallback OnClose { get; set; }

    /// <summary>Modal title shown in the header. If null, the header is hidden.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Size variant controlling the dialog width.</summary>
    [Parameter] public ModalSize Size { get; set; } = ModalSize.Medium;

    /// <summary>Default close and footer-action behavior.</summary>
    [Parameter] public FormaModalOptions Options { get; set; } = FormaModalOptions.Default;

    /// <summary>Main content of the modal.</summary>
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
    private string SizeClass => $"forma-modal-{Size.ToString().ToLowerInvariant()}";
    private string ConfirmLabel => ConfirmText ?? L["Confirm"];
    private string CancelLabel =>
        CancelText ?? (Options.ShowCancelAction && !Options.ShowConfirmAction ? L["Close"] : L["Cancel"]);

    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Options);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _module = await JS.InvokeAsync<IJSObjectReference>(
                    "import", "./_content/Forma.Blazor/Components/Admin/Feedback/FormaModal.razor.js").ConfigureAwait(false);
            }
            catch (JSException)
            {
                // Circuit disconnected before module loaded — safe to ignore.
                return;
            }
        }

        if (_module is null)
            return;

        try
        {
            if (Opened && !_isOpen)
            {
                _isOpen = true;
                await _module.InvokeVoidAsync("showModal", _dialogRef, Options.CloseOnBackdropClick).ConfigureAwait(false);
            }
            else if (!Opened && _isOpen)
            {
                _isOpen = false;
                await _module.InvokeVoidAsync("closeModal", _dialogRef).ConfigureAwait(false);
            }
        }
        catch (JSException)
        {
            // Module disposed concurrently (navigation during open dialog) — safe to ignore.
        }
    }

    /// <summary>
    /// Called when the native dialog fires its <c>close</c> event (Escape key or <c>.close()</c>).
    /// Notifies the parent to update <see cref="Opened"/>.
    /// </summary>
    private async Task HandleDialogClose()
    {
        if (_isOpen)
        {
            _isOpen = false;
            await OnClose.InvokeAsync().ConfigureAwait(false);
        }
    }

    private async Task RequestCloseAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("closeModal", _dialogRef).ConfigureAwait(false);
            }
            catch (JSException)
            {
                // Module disposed concurrently — safe to ignore.
            }
            return;
        }

        if (_isOpen)
        {
            _isOpen = false;
            await OnClose.InvokeAsync().ConfigureAwait(false);
        }
    }

    private async Task HandleDefaultCancelAsync()
    {
        if (OnCancel.HasDelegate)
            await OnCancel.InvokeAsync().ConfigureAwait(false);

        if (Options.CloseOnCancel)
            await RequestCloseAsync().ConfigureAwait(false);
    }

    private async Task HandleDefaultConfirmAsync()
    {
        if (OnConfirm.HasDelegate)
            await OnConfirm.InvokeAsync().ConfigureAwait(false);

        if (Options.CloseOnConfirm)
            await RequestCloseAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        var module = _module;
        _module = null;
        if (module is not null)
        {
            try
            {
                await module.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
                // Circuit disconnected — safe to ignore.
            }
        }
    }
}
