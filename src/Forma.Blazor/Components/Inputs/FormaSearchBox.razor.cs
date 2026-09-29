namespace Forma.Blazor.Components.Inputs;

using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;
using Forma.Framework.Timing;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A search field with a leading glyph, a clear button once populated, and an optional debounce before it reports a search.</summary>
public sealed partial class FormaSearchBox : FormaElementBase
{
    private const string SearchAction = "search-box.search";

    // Enter runs the search at once, a route on the field itself so it wins over the field's own claim on the key and the form around it never takes it as a submission.
    private static readonly InputProfile SearchProfile = new()
    {
        InputRoutes = [new InputRoute(SearchAction, new KeyBinding("Enter"))],
    };

    private InputRegistration _inputRegistration = default!;

    /// <summary>The current text.</summary>
    [Parameter] public string Value { get; set; } = string.Empty;

    /// <summary>Raised on every keystroke, so a caller can keep a bound value in step as the user types.</summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Raised with the committed search term: instantly, or after the configured debounce delay once typing pauses. Pressing Enter or clearing the field bypasses the wait.</summary>
    [Parameter] public EventCallback<string> OnSearch { get; set; }

    /// <summary>How the field presents itself and when it reports a search.</summary>
    [Parameter] public SearchBoxOptions Options { get; set; } = SearchBoxOptions.Default;

    [Inject] private IDebounceable Debouncer { get; set; } = default!;

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private string EffectivePlaceholder
        => Options.Placeholder ?? L["Search..."].Value;

    /// <inheritdoc />
    protected override void OnInitialized()
        => _inputRegistration = new InputRegistration(InputRouter, HandleActionAsync);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Options);
        ArgumentOutOfRangeException.ThrowIfLessThan(Options.DebounceDelay, TimeSpan.Zero, nameof(Options));
    }

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
        => _inputRegistration.SyncAsync(RootElement, SearchProfile);

    private async Task HandleInputAsync(ChangeEventArgs args)
    {
        Value = args.Value?.ToString() ?? string.Empty;

        await ValueChanged.InvokeAsync(Value);
        await Debouncer.DebounceAsync(_ => OnSearch.InvokeAsync(Value), Options.DebounceDelay);
    }

    private async Task HandleActionAsync(string action, InputInfo input)
    {
        if (action != SearchAction)
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
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-search-box");

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        Debouncer.Cancel();
        await _inputRegistration.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
