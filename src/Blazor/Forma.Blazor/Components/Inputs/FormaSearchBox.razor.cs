namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Browser.Interop.Text;
using Forma.Framework.Css;
using Forma.Framework.Events;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A search field with a leading glyph and a clear button that reports a search as the user types.</summary>
public sealed partial class FormaSearchBox : FormaElementBase
{
    private const string SearchAction = "search-box.search";

    // Enter runs the search at once through a route on the field itself, which takes the key ahead of the field's own claim and of the form's submission.
    private static readonly InputProfile SearchProfile = new()
    {
        InputRoutes = [new InputRoute(SearchAction, new KeyBinding("Enter"))],
    };

    // A search reaches the component as each character is typed.
    private static readonly TextEditOptions EditOptions = new() { SendsOnInput = true };

    private InputRegistration _inputRegistration = default!;
    private DomTextEdits _edits = default!;

    /// <summary>The current text.</summary>
    [Parameter] public string Value { get; set; } = string.Empty;

    /// <summary>Raised with the text on every keystroke.</summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Raised with the committed search term.</summary>
    /// <remarks>Typing reports the term once it pauses for the debounce delay, or at every keystroke where the delay is zero; Enter and clearing the field report it at once.</remarks>
    [Parameter] public EventCallback<string> OnSearch { get; set; }

    /// <summary>The label pinned at the top of the field.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>How the field presents itself and when it reports a search.</summary>
    [Parameter] public SearchBoxOptions Options { get; set; } = SearchBoxOptions.Default;

    [Inject] private IDebounceable Debouncer { get; set; } = default!;

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    // The value the box renders, which a render never writes back over the reader's typing.
    private string? ShownText
        => _edits.GetShownText(Value);

    private string EffectivePlaceholder
        => Options.Placeholder ?? L["Search..."].Value;

    private bool HasLabel
        => !string.IsNullOrWhiteSpace(Label);

    private string? WrapperClass
        => new CssClassBuilder().Add("forma-search-box-wrapper").Add("has-label", HasLabel).ToStringOrNull();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _inputRegistration = new InputRegistration(InputRouter, HandleActionAsync);
        _edits = new DomTextEdits(Context.DomEngine, Value, HandleTextChangedAsync);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Options);
        ArgumentOutOfRangeException.ThrowIfLessThan(Options.DebounceDelay, TimeSpan.Zero, nameof(Options));
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await _edits.SyncAsync(RootElement, Value, EditOptions);

        await _inputRegistration.SyncAsync(RootElement, SearchProfile);
    }

    private async Task HandleTextChangedAsync(TextChange change)
    {
        if (change.Text == Value)
        {
            return;
        }

        Value = change.Text;
        StateHasChanged();
        await ValueChanged.InvokeAsync(Value);

        // The search waits out its delay apart from the edit, so the next keystroke's edit is applied meanwhile and restarts it.
        _ = SearchAfterDelayAsync();
    }

    [SuppressMessage("Design", "CA1031", Justification = "The host's search failing is the component's own failure, handed to the renderer as it would be from an event.")]
    private async Task SearchAfterDelayAsync()
    {
        try
        {
            // The delay elapses on the clock's thread, so the search goes back to the renderer to run.
            await Debouncer.DebounceAsync(_ => InvokeAsync(() => OnSearch.InvokeAsync(Value)), Options.DebounceDelay);
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private async Task HandleActionAsync(string action, InputInfo input)
    {
        // The search reads the text the reader typed, every edit sent ahead of the key applied.
        if (action != SearchAction || !await _edits.SettleAsync())
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
        => builder
            .Add("forma-field-input", HasLabel)
            .Add("forma-search-box");

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        Debouncer.Cancel();

        await _edits.DisposeAsync();

        await _inputRegistration.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
