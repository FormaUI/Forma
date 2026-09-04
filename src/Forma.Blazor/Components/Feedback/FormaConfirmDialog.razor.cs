namespace Forma.Blazor.Components.Feedback;

using Forma.Features.Feedback;

using Microsoft.AspNetCore.Components;

/// <summary>The host that renders confirmation requests raised through <see cref="IConfirmDialogService"/>.</summary>
public sealed partial class FormaConfirmDialog : IDisposable
{
    private static readonly DialogOptions DialogOptions =
        new() { CloseOnBackdropClick = false, ShowCancelAction = false, ShowConfirmAction = false };

    private ConfirmDialogRequest? _request;
    private bool _isOpen;

    [Inject] private IConfirmDialogService ConfirmService { get; set; } = default!;

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
    public void Dispose() => ConfirmService.Unsubscribe(HandleShowRequestedAsync);
}
