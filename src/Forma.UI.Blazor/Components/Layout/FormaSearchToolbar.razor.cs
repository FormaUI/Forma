namespace Forma.UI.Blazor.Components.Layout;

using Microsoft.AspNetCore.Components;

/// <summary>A search field and action buttons in one row, for the top of a list page.</summary>
public sealed partial class FormaSearchToolbar : IDisposable
{
    private CancellationTokenSource? _cts;

    /// <summary>The action buttons shown beside the search field.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Raised with the search text once typing settles; the field is omitted when unset.</summary>
    [Parameter] public EventCallback<string> OnSearch { get; set; }

    /// <summary>The search field's placeholder and accessible name.</summary>
    [Parameter] public string? SearchPlaceholder { get; set; }

    /// <summary>How long typing must settle before OnSearch is raised.</summary>
    [Parameter] public TimeSpan SearchDelay { get; set; } = TimeSpan.FromMilliseconds(300);

    protected override void OnParametersSet() => ArgumentOutOfRangeException.ThrowIfLessThan(SearchDelay, TimeSpan.Zero);

    private async Task HandleSearchInput(ChangeEventArgs e)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        var value = e.Value?.ToString() ?? "";

        await Task.Delay(SearchDelay, token)
                  .ContinueWith(t =>
                  {
                      if (t.IsCompletedSuccessfully)
                      {
                          return InvokeAsync(() => OnSearch.InvokeAsync(value));
                      }

                      return Task.CompletedTask;
                  }, TaskScheduler.Default)
                  .Unwrap();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
