namespace Forma.Blazor.Components.Inputs;

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

    private InputRegistration _inputRegistration = default!;

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

    private string EffectivePlaceholder
        => Options.Placeholder ?? L["Search..."].Value;

    private bool HasLabel
        => !string.IsNullOrWhiteSpace(Label);

    private string? WrapperClass
        => new CssClassBuilder().Add("forma-search-box-wrapper").Add("has-label", HasLabel).ToStringOrNull();

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

    private async Task HandleInputAsync(string? text)
    {
        Value = text ?? string.Empty;

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
        => builder
            .Add("forma-field-input", HasLabel)
            .Add("forma-search-box");

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        Debouncer.Cancel();
        await _inputRegistration.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
