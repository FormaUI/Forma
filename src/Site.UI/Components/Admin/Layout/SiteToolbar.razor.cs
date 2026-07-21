using Microsoft.AspNetCore.Components;

namespace Site.UI.Components.Admin.Layout;

public sealed partial class SiteToolbar : IDisposable
{
    private CancellationTokenSource? _cts;

    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public EventCallback<string> OnSearch { get; set; }
    [Parameter] public string? SearchPlaceholder { get; set; }
    [Parameter] public TimeSpan SearchDelay { get; set; } = TimeSpan.FromMilliseconds(300);

    protected override void OnParametersSet()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(SearchDelay, TimeSpan.Zero);
    }

    private async Task HandleSearchInput(ChangeEventArgs e)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
            _cts.Dispose();
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        var value = e.Value?.ToString() ?? "";

        await Task.Delay(SearchDelay, token)
                  .ContinueWith(t =>
                  {
                      if (t.IsCompletedSuccessfully)
                          return InvokeAsync(() => OnSearch.InvokeAsync(value));
                      return Task.CompletedTask;
                  }, TaskScheduler.Default)
                  .Unwrap()
                  .ConfigureAwait(false);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
