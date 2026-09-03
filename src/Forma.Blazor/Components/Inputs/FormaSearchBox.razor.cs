namespace Forma.Blazor.Components.Inputs;

using Forma.Framework.Localization;
using Forma.Framework.Timing;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A search field with a leading glyph, a clear button once populated, and an optional debounce before it reports a search.</summary>
public sealed partial class FormaSearchBox : ComponentBase, IAsyncDisposable
{
    /// <summary>The current text.</summary>
    [Parameter] public string Value { get; set; } = string.Empty;

    /// <summary>Raised on every keystroke, so a caller can keep a bound value in step as the user types.</summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Raised with the committed search term: instantly, or after the configured debounce delay once typing pauses. Pressing Enter or clearing the field bypasses the wait.</summary>
    [Parameter] public EventCallback<string> OnSearch { get; set; }

    /// <summary>How the field presents itself and when it reports a search.</summary>
    [Parameter] public SearchBoxOptions Options { get; set; } = SearchBoxOptions.Default;

    /// <summary>The HTML element identifier.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Extra classes applied alongside forma-search-box.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Additional attributes applied to the input element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>The rendered input element, for a composing component that must focus it.</summary>
    public ElementReference Element { get; private set; }

    [Inject] private IStringLocale<FormaSearchBox> L { get; set; } = default!;

    [Inject] private IDebounceable Debouncer { get; set; } = default!;

    private string InputClass
        => string.IsNullOrWhiteSpace(Class) ? "forma-search-box" : $"forma-search-box {Class}";

    private string EffectivePlaceholder
        => Options.Placeholder ?? L["Search..."].Value;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Options);
        ArgumentOutOfRangeException.ThrowIfLessThan(Options.DebounceDelay, TimeSpan.Zero, nameof(Options));
    }

    private async Task HandleInputAsync(ChangeEventArgs args)
    {
        Value = args.Value?.ToString() ?? string.Empty;

        await ValueChanged.InvokeAsync(Value);
        await Debouncer.DebounceAsync(_ => OnSearch.InvokeAsync(Value), Options.DebounceDelay);
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key != "Enter")
        {
            return;
        }

        Debouncer.Cancel();
        await OnSearch.InvokeAsync(Value);
    }

    private async Task ClearAsync()
    {
        Value = string.Empty;

        await ValueChanged.InvokeAsync(Value);
        Debouncer.Cancel();
        await OnSearch.InvokeAsync(Value);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Debouncer.Cancel();

        return ValueTask.CompletedTask;
    }
}
