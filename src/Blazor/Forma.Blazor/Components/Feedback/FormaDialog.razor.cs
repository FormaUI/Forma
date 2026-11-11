namespace Forma.Blazor.Components.Feedback;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Browser.Interop.Components;
using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>A modal dialog in the browser's top layer, with a header, a body and footer actions.</summary>
/// <remarks>While open, the dialog is an active modal scope of the input router, which decides Escape and a platform close request and dismisses the dialog through it.</remarks>
public sealed partial class FormaDialog : FormaElementBase
{
    /// <summary>The selector of the part whose first control takes focus as the dialog opens.</summary>
    private const string BodySelector = ".forma-dialog-body";

    private static readonly InputProfile ModalProfile = new()
    {
        Modal = true,
        Escape = DismissBehavior.Dismiss,
        CloseRequest = DismissBehavior.Dismiss,
        PressAway = PressAwayBehavior.Ignore,
    };

    private Dialog _dialog = default!;
    private InputRegistration _inputRegistration = default!;
    private bool _isOpen;
    private bool _hasOpened;

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    /// <summary>Whether the dialog is open.</summary>
    [Parameter, EditorRequired] public bool Opened { get; set; }

    /// <summary>The callback invoked when the dialog closes.</summary>
    [Parameter, EditorRequired] public EventCallback OnClose { get; set; }

    /// <summary>The title shown in the header, where null hides the header.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>The size of the dialog.</summary>
    [Parameter] public DialogSize Size { get; set; } = DialogSize.Medium;

    /// <summary>The close and footer-action options of the dialog.</summary>
    [Parameter] public DialogOptions Options { get; set; } = DialogOptions.Default;

    /// <summary>The content of the dialog's body.</summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>The content of the dialog's footer.</summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>The text of the default confirm action.</summary>
    [Parameter] public string? ConfirmText { get; set; }

    /// <summary>The text of the default cancel action.</summary>
    [Parameter] public string? CancelText { get; set; }

    /// <summary>The callback invoked by the default confirm action.</summary>
    [Parameter] public EventCallback OnConfirm { get; set; }

    /// <summary>The callback invoked by the default cancel action.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    private bool ShowHeader => Title is not null;
    private string TitleId => $"{ElementId}-title";
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
        _dialog = new Dialog(Context.JS);
        _inputRegistration = new InputRegistration(InputRouter, onDismiss: HandleDismissAsync);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Options);

        if (!Opened)
        {
            _hasOpened = false;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _dialog.AttachAsync(RootElement);
        }

        // An Opened request is honored once; a dialog the user closed stays closed until the parent asks again.
        if (Opened && !_isOpen && !_hasOpened)
        {
            // Only a call that ran counts as open; one made before the module answered is retried on the next render.
            _isOpen = await _dialog.OpenAsync(Options.CloseOnScrimClick, BodySelector);

            if (_isOpen)
            {
                _hasOpened = true;
            }
        }
        else if (!Opened && _isOpen)
        {
            await SetClosedAsync();
            await _dialog.CloseAsync();
        }

        // The dialog is an active scope exactly while open, standing as the modal barrier.
        await _inputRegistration.SyncAsync(RootElement, ModalProfile, shouldRegister: _isOpen, isActive: _isOpen);
    }

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "The router hands every owner the action and the reason.")]
    private Task HandleDismissAsync(DismissBehavior action, DismissReason reason)
        => action switch
        {
            DismissBehavior.Dismiss => RequestCloseAsync(),
            DismissBehavior.Cancel => HandleDefaultCancelAsync(),
            _ => Task.CompletedTask,
        };

    /// <summary>Handles the native dialog's <c>close</c> event.</summary>
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

        // A close call that never ran fires no close event, so the state closes here.
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

    // The scope goes before the dialog does, so nothing is routed to a dialog on its way out.
    private async Task SetClosedAsync()
    {
        _isOpen = false;
        await _inputRegistration.SyncAsync(RootElement, ModalProfile, shouldRegister: false);
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-dialog")
            .Add(SizeClass);

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        _isOpen = false;
        await _inputRegistration.DisposeAsync();
        await _dialog.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
