namespace Forma.UI.Blazor.Components.Inputs;

using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A search field with a leading glyph, a clear button once populated, and an optional debounce before it reports a search.</summary>
public sealed partial class FormaSearchBox : ComponentBase, IAsyncDisposable
{
    /// <summary>The current text.</summary>
    [Parameter] public string Value { get; set; } = string.Empty;

    /// <summary>Raised on every keystroke, so a caller can keep a bound value in step as the user types.</summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Raised with the committed search term: instantly, or after DebounceDelay once typing pauses. Pressing Enter or clearing the field bypasses the wait.</summary>
    [Parameter] public EventCallback<string> OnSearch { get; set; }

    /// <summary>How long to wait after the last keystroke before raising OnSearch. Null, the default, reports on every keystroke.</summary>
    [Parameter] public TimeSpan? DebounceDelay { get; set; }

    /// <summary>Placeholder text shown when the field is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>The HTML element identifier.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Whether the field is disabled and ignores user input.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Extra classes applied alongside forma-search-box.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Additional attributes applied to the input element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>The rendered input element, for a composing component that must focus it.</summary>
    public ElementReference Element { get; private set; }

    [Inject] private IStringLocale<FormaSearchBox> L { get; set; } = default!;

    private CancellationTokenSource? _debounceCancellation;

    private string InputClass
        => string.IsNullOrWhiteSpace(Class) ? "forma-search-box" : $"forma-search-box {Class}";

    private string EffectivePlaceholder
        => Placeholder ?? L["Search..."].Value;

    private async Task HandleInputAsync(ChangeEventArgs args)
    {
        Value = args.Value?.ToString() ?? string.Empty;

        await ValueChanged.InvokeAsync(Value);
        await RaiseSearchAsync(Value);
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key != "Enter")
        {
            return;
        }

        CancelDebounce();
        await OnSearch.InvokeAsync(Value);
    }

    private async Task ClearAsync()
    {
        Value = string.Empty;

        await ValueChanged.InvokeAsync(Value);
        CancelDebounce();
        await OnSearch.InvokeAsync(Value);
    }

    private async Task RaiseSearchAsync(string value)
    {
        if (DebounceDelay is not { } delay || delay <= TimeSpan.Zero)
        {
            CancelDebounce();
            await OnSearch.InvokeAsync(value);
            return;
        }

        var token = RestartDebounce();

        try
        {
            await Task.Delay(delay, token);
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
        CancelDebounce();
        _debounceCancellation = new CancellationTokenSource();

        return _debounceCancellation.Token;
    }

    private void CancelDebounce()
    {
        _debounceCancellation?.Cancel();
        _debounceCancellation?.Dispose();
        _debounceCancellation = null;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        CancelDebounce();

        return ValueTask.CompletedTask;
    }
}
