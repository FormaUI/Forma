using Forma.Blazor.Abstractions.Components.Feedback;
using Microsoft.AspNetCore.Components;

namespace Forma.Blazor.Components.Admin.Feedback;

public sealed partial class FormaConfirmDialog : IDisposable
{
    private ConfirmDialogRequest? _request;
    private bool _isOpen;

    [Inject] private IConfirmDialogService ConfirmService { get; set; } = default!;

    protected override void OnInitialized()
    {
        ConfirmService.Subscribe(HandleShowRequestedAsync);
    }

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

    public void Dispose()
    {
        ConfirmService.Unsubscribe(HandleShowRequestedAsync);
    }
}
