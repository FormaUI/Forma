namespace Forma.UI.Blazor.Components.Bars;

using Forma.UI.Features.Toolbar;

using Microsoft.AspNetCore.Components;

/// <summary>A search field and a row of toolbar items, for the top of a list page.</summary>
public sealed partial class FormaSearchToolbar : IDisposable
{
    private CancellationTokenSource? _cts;

    /// <summary>The accessible name of the action toolbar; required only when Items is not empty.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The items shown beside the search field.</summary>
    [Parameter] public IReadOnlyList<FormaToolbarItem> Items { get; set; } = [];

    /// <summary>Callback invoked with the command an action raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>Raised with the search text once typing settles.</summary>
    [Parameter, EditorRequired] public EventCallback<string> OnSearch { get; set; }

    /// <summary>The search field's placeholder and accessible name.</summary>
    [Parameter, EditorRequired] public string SearchPlaceholder { get; set; } = default!;

    /// <summary>How long typing must settle before OnSearch is raised.</summary>
    [Parameter] public TimeSpan SearchDelay { get; set; } = TimeSpan.FromMilliseconds(300);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(SearchDelay, TimeSpan.Zero);
        ArgumentException.ThrowIfNullOrWhiteSpace(SearchPlaceholder);

        if (!OnSearch.HasDelegate)
        {
            throw new ArgumentException("A search toolbar must handle OnSearch; use FormaToolbar for a row of actions alone.", nameof(OnSearch));
        }

        if (Items.Count > 0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        }
    }

    private async Task HandleSearchInput(ChangeEventArgs e)
    {
        var token = RestartDebounce();
        var value = e.Value?.ToString() ?? "";

        try
        {
            await Task.Delay(SearchDelay, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await OnSearch.InvokeAsync(value);
    }

    // Cancelling synchronously keeps the swap atomic on the dispatcher; awaiting would let a second keystroke interleave and strand a token.
    private CancellationToken RestartDebounce()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        return _cts.Token;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
