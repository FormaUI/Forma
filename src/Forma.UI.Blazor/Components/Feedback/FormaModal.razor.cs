namespace Forma.UI.Blazor.Components.Feedback;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Localization;
using Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public sealed partial class FormaModal : IAsyncDisposable
{
    private ElementReference _dialogRef;
    private ModalControl _modal = default!;
    private bool _isOpen;

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IStringLocale<FormaModal> L { get; set; } = default!;

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

    protected override void OnInitialized()
        => _modal = new ModalControl(JS);

    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Options);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _modal.AttachAsync(_dialogRef);
        }

        if (Opened && !_isOpen)
        {
            _isOpen = true;
            await _modal.OpenAsync(Options.CloseOnBackdropClick);
        }
        else if (!Opened && _isOpen)
        {
            _isOpen = false;
            await _modal.CloseAsync();
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
            await OnClose.InvokeAsync();
        }
    }

    private async Task RequestCloseAsync()
    {
        if (await _modal.CloseAsync())
        {
            return;
        }

        // The dialog's own close event cannot fire when the call never ran, so the state
        // is closed here instead.
        if (_isOpen)
        {
            _isOpen = false;
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

    public async ValueTask DisposeAsync()
        => await _modal.DisposeAsync();
}
