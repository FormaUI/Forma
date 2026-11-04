namespace Forma.Blazor.Components.Inputs;

using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Components.Virtualization;
using Forma.Framework.Browser.Interop.Text;
using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;

using Microsoft.AspNetCore.Components;

/// <summary>A field for typing a time of day, with a list of suggested times that opens beneath it, as an editable combobox.</summary>
public sealed partial class FormaTimeBox : FormaComponentBase
{
    private const string NextAction = "time-box.next";
    private const string PreviousAction = "time-box.previous";
    private const string ChooseAction = "time-box.choose";

    // The vertical arrows open the list and move through it; Enter takes the highlighted time only while the list is open, leaving it to
    // the form otherwise. Escape is the popover's, which closes the list without taking a time.
    private static readonly InputProfile ClosedProfile = new()
    {
        InputRoutes =
        [
            new InputRoute(NextAction, new KeyBinding("ArrowDown")),
            new InputRoute(PreviousAction, new KeyBinding("ArrowUp")),
        ],
    };

    private static readonly InputProfile OpenProfile = new()
    {
        InputRoutes =
        [
            new InputRoute(NextAction, new KeyBinding("ArrowDown")),
            new InputRoute(PreviousAction, new KeyBinding("ArrowUp")),
            new InputRoute(ChooseAction, new KeyBinding("Enter")),
        ],
    };

    /// <summary>The id of the field's text element, which a label names.</summary>
    [Parameter, EditorRequired] public string ElementId { get; set; } = default!;

    /// <summary>The runtime id of the component the box edits for.</summary>
    [Parameter] public Guid OwnerRuntimeId { get; set; }

    /// <summary>The text the field shows.</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>The time the field holds, which the list opens on.</summary>
    [Parameter] public TimeOnly? Time { get; set; }

    /// <summary>The times the list suggests, in the order it shows them.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<TimeOnly> Suggestions { get; set; } = default!;

    /// <summary>Writes a suggested time as the list shows it.</summary>
    [Parameter, EditorRequired] public Func<TimeOnly, string> FormatTime { get; set; } = default!;

    /// <summary>The list's accessible name.</summary>
    [Parameter, EditorRequired] public string ListLabel { get; set; } = default!;

    /// <summary>Raised with the field's text as the reader types it.</summary>
    [Parameter] public EventCallback<string?> OnInput { get; set; }

    /// <summary>Raised with the field's text once the reader has changed it.</summary>
    [Parameter] public EventCallback<string?> OnChange { get; set; }

    /// <summary>Raised with the suggested time the reader chose.</summary>
    [Parameter] public EventCallback<TimeOnly> OnChoose { get; set; }

    /// <summary>The placeholder text shown when the field is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Whether the field is marked as required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Whether the field is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the field is read-only.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>The classes the field's text element carries.</summary>
    [Parameter] public string? InputClass { get; set; }

    /// <summary>The style the field's text element carries.</summary>
    [Parameter] public string? InputStyle { get; set; }

    /// <summary>The attributes the field's text element carries.</summary>
    [Parameter] public IReadOnlyDictionary<string, object>? InputAttributes { get; set; }

    /// <summary>The space-separated element IDs used for the text element's aria-describedby attribute.</summary>
    [Parameter] public string? InputDescribedBy { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    // Each edit reaches the owner as it is typed, and the field's commit as its change.
    private static readonly TextEditOptions EditOptions = new() { SendsOnInput = true };

    private InputRegistration _inputRegistration = default!;
    private DomTextEdits _edits = default!;
    private ElementReference _anchorElement;
    private IReadOnlyList<TimeOnly>? _optionsSource;
    private TimeOption[] _options = [];
    private int _activeIndex = -1;

    // The field renders the text the reader last entered until the owner hands it a different one, since the owner's text arrives a render later.
    private string? _text;
    private string? _receivedText;

    /// <summary>The field's text element, once rendered.</summary>
    public ElementReference InputElement { get; private set; }

    private FormaPopover Popover { get; set; } = default!;

    private FormaVirtualize<TimeOption>? Virtualizer { get; set; }

    private bool IsOpen
        => Popover is { IsOpen: true };

    // The value the field renders, which a render never writes back over the reader's typing.
    private string? ShownText
        => _edits.GetShownText(_text);

    private string ListId
        => $"{ElementId}-list";

    private string? ActiveOptionId
        => IsOpen && _activeIndex >= 0 ? GetOptionId(_activeIndex) : null;

    // The highlighted time stays drawn wherever the list scrolls, so the field always names an element that is there.
    private object[] PinnedIndexes
        => _activeIndex >= 0 ? [_activeIndex] : [];

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _inputRegistration = new InputRegistration(InputRouter, HandleActionAsync);
        _edits = new DomTextEdits(Context.DomEngine, Text, HandleTextChangedAsync);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ElementId);
        ArgumentNullException.ThrowIfNull(Suggestions);
        ArgumentNullException.ThrowIfNull(FormatTime);

        if (Text != _receivedText)
        {
            _receivedText = Text;
            _text = Text;
        }

        if (!ReferenceEquals(_optionsSource, Suggestions))
        {
            _optionsSource = Suggestions;
            _options = [.. Suggestions.Select((time, index) => new TimeOption(time, index))];
            _activeIndex = Math.Min(_activeIndex, _options.Length - 1);
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await _edits.SyncAsync(InputElement, _text, EditOptions);

        await _inputRegistration.SyncAsync(InputElement, IsOpen ? OpenProfile : ClosedProfile);
    }

    private string GetOptionId(int index)
        => $"{ElementId}-option-{index}";

    private string GetOptionClass(TimeOption option)
    {
        var builder = new CssClassBuilder();
        builder.Add("forma-time-box-option");
        builder.Add("forma-time-box-option-active", option.Index == _activeIndex);
        builder.Add("forma-time-box-option-current", option.Time == Time);

        return builder.ToStringOrNull()!;
    }

    private async Task HandleTextChangedAsync(TextChange change)
    {
        var isEdited = change.Text != (_text ?? string.Empty);
        _text = change.Text;

        if (isEdited)
        {
            await OnInput.InvokeAsync(change.Text);
        }

        if (change.IsCommit)
        {
            await OnChange.InvokeAsync(change.Text);
        }
    }

    // Leaving the field closes the list; a press on the list or the button keeps the focus, so only a move elsewhere leaves.
    private Task HandleFocusOutAsync()
        => IsOpen ? Popover.CloseAsync() : Task.CompletedTask;

    private async Task HandleActionAsync(string action, InputInfo input)
    {
        if (Disabled || ReadOnly || _options.Length == 0)
        {
            return;
        }

        switch (action)
        {
            case NextAction when !IsOpen:
                await OpenAsync(isForward: true);
                break;
            case PreviousAction when !IsOpen:
                await OpenAsync(isForward: false);
                break;
            case NextAction:
                await MoveAsync(1);
                break;
            case PreviousAction:
                await MoveAsync(-1);
                break;
            case ChooseAction when IsOpen && _activeIndex >= 0:
                await ChooseAsync(_options[_activeIndex].Time);
                break;
            default:
                break;
        }
    }

    private Task ToggleAsync()
        => IsOpen ? Popover.CloseAsync() : OpenAsync(isForward: true);

    // The list opens on the suggestion nearest the time the field holds, or at the end the key moves from where it holds none.
    private Task OpenAsync(bool isForward)
    {
        _activeIndex = GetNearestIndex() ?? (isForward ? 0 : _options.Length - 1);

        return Popover.OpenAsync(InputElement);
    }

    private async Task MoveAsync(int step)
    {
        _activeIndex = Math.Clamp(_activeIndex + step, 0, _options.Length - 1);
        StateHasChanged();

        if (Virtualizer is { } virtualizer)
        {
            await virtualizer.ScrollToIndexAsync(_activeIndex);
        }
    }

    private async Task ChooseAsync(TimeOnly time)
    {
        await OnChoose.InvokeAsync(time);
        await Popover.CloseAsync();
    }

    private void HandleOpenChanged(bool isOpen)
    {
        if (!isOpen)
        {
            _activeIndex = -1;
        }
    }

    // The suggestion a clock face puts nearest the time held, the distance read both ways round midnight.
    private int? GetNearestIndex()
    {
        if (Time is not { } time)
        {
            return null;
        }

        var nearest = 0;
        var nearestDistance = long.MaxValue;

        foreach (var option in _options)
        {
            var distance = Math.Abs(option.Time.Ticks - time.Ticks);
            distance = Math.Min(distance, TimeSpan.TicksPerDay - distance);

            if (distance < nearestDistance)
            {
                nearest = option.Index;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _edits.DisposeAsync();

        await _inputRegistration.DisposeAsync();
        await base.DisposeAsyncCore();
    }

    // A suggested time and its place in the list, which keys the option and names its element.
    private readonly record struct TimeOption(TimeOnly Time, int Index);
}
