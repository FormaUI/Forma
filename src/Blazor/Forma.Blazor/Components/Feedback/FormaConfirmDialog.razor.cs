namespace Forma.Blazor.Components.Feedback;

using Forma.Framework.Css;
using Forma.Framework.Feedback;

using Microsoft.AspNetCore.Components;

/// <summary>The host that renders confirmation requests raised through <see cref="IConfirmDialogService"/>.</summary>
public sealed partial class FormaConfirmDialog : FormaElementBase
{
    private static readonly DialogOptions DialogOptions =
        new() { CloseOnScrimClick = false, ShowCancelAction = false, ShowConfirmAction = false };

    private ConfirmDialogRequest? _request;
    private bool _isOpen;

    [Inject] private IConfirmDialogService ConfirmService { get; set; } = default!;

    private FormaDialog Dialog { get; set; } = default!;

    /// <inheritdoc />
    public override ElementReference RootElement
        => Dialog is null ? default : Dialog.RootElement;

    /// <inheritdoc />
    protected override void OnInitialized() => ConfirmService.Subscribe(HandleShowRequestedAsync);

    private Task HandleShowRequestedAsync()
    {
        return InvokeAsync(() =>
        {
            _request = ConfirmService.Current;
            _isOpen = _request is not null;
            StateHasChanged();
        });
    }

    private void HandleConfirm()
    {
        _request?.Result.TrySetResult(true);
        Close();
    }

    private void HandleCancel()
    {
        _request?.Result.TrySetResult(false);
        Close();
    }

    private void Close()
    {
        _isOpen = false;
        _request = null;
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-confirm-dialog");

    /// <inheritdoc />
    protected override ValueTask DisposeAsyncCore()
    {
        ConfirmService.Unsubscribe(HandleShowRequestedAsync);
        return base.DisposeAsyncCore();
    }
}
