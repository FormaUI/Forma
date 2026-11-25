namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Components.Interaction;
using Forma.Blazor.Components.Virtualization;
using Forma.Framework.Browser;
using Forma.Framework.Browser.Interop;
using Forma.Framework.Browser.Interop.Text;
using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;

using Microsoft.AspNetCore.Components;

/// <summary>A form input choosing one value from a list that narrows as the reader types, which can take the typed text as a value the list doesn't hold.</summary>
/// <typeparam name="TValue">The value the input binds.</typeparam>
/// <remarks>
/// The bound value, the field's text and the option the list holds active are three states. Typing changes the text, which narrows
/// the list and makes its first option that can be chosen the active one; the arrows move the active option. Only choosing an option,
/// adding one, or leaving the field on text that names exactly one option that can be chosen commits a value; leaving on any other text
/// restores the committed value's label, and leaving on no text clears a value that can be empty. A value bound from outside while the
/// reader edits replaces the edit.
/// </remarks>
public sealed partial class FormaComboBox<TValue> : FormaInputBase<TValue>
{
    private const string NextAction = "combobox.next";
    private const string PreviousAction = "combobox.previous";
    private const string ChooseAction = "combobox.choose";
    private const string RestoreAction = "combobox.restore";

    // An exact match ignores case, accents and width, as the ranking does.
    private const CompareOptions Loose = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType;

    // The vertical arrows open the list and move through it; Enter takes the active option only while the list is open, leaving it to
    // the form otherwise. The first Escape is the popover's, which closes the list; one on the closed list restores the committed text.
    private static readonly InputProfile ClosedProfile = new()
    {
        InputRoutes =
        [
            new InputRoute(NextAction, new KeyBinding("ArrowDown")),
            new InputRoute(PreviousAction, new KeyBinding("ArrowUp")),
        ],
    };

    private static readonly InputProfile EditedClosedProfile = new()
    {
        InputRoutes =
        [
            new InputRoute(NextAction, new KeyBinding("ArrowDown")),
            new InputRoute(PreviousAction, new KeyBinding("ArrowUp")),
            new InputRoute(RestoreAction, new KeyBinding("Escape")),
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

    // Each edit reaches the component as it's typed, so the list narrows with every key.
    private static readonly TextEditOptions EditOptions = new() { SendsOnInput = true };

    private static readonly FocusWatchOptions FocusOptions = new();

    // Whether a value can be empty, which leaving the field on no text makes it.
    private static readonly bool CanBeEmpty = default(TValue) is null;

    // Whether an option's value posts as text the server's form binding reads back, which a statically rendered page's own list needs.
    private static readonly bool IsPostable = FormValue.IsPostable<TValue>();

    /// <summary>The options the list offers, in the order it shows them while nothing is typed.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<SelectOption<TValue>> Options { get; set; } = default!;

    /// <summary>The field's label.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Whether the field is marked as required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Whether the field is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the field is read-only.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>The text shown in the field while it holds none.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>The label a value no option holds goes by, where null uses the value's own text in the culture.</summary>
    [Parameter] public Func<TValue, string>? ValueLabel { get; set; }

    /// <summary>Creates the option for text no option names, which the field then commits, or answers null to refuse it.</summary>
    /// <remarks>The list offers to add typed text only where the factory is set. The field commits the option it answers and shows its label for as long as it holds the option's value, whether or not <see cref="Options"/> comes to hold the option.</remarks>
    [Parameter] public Func<string, Task<SelectOption<TValue>?>>? OptionFactory { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private InputRegistration _inputRegistration = default!;
    private DomTextEdits _edits = default!;
    private IDomObservation<FocusWatchOptions, FocusState>? _focusState;
    private ElementReference _anchorElement;
    private bool _isWithin;
    private bool _isOutside;
    private bool _isTextFaulted;

    // The edit in progress: the text typed, the value it began over, which a different value bound from outside replaces.
    private bool _isEdited;
    private string? _text;
    private TValue? _valueWhenEdited;

    // The entry the list holds active, by its place in the list, and the entries as last ranked, for the text, options and culture
    // they were ranked for.
    private int _activeIndex = -1;
    private IReadOnlyList<ListEntry> _entries = [];
    private (string? Text, bool IsEdited, IReadOnlyList<SelectOption<TValue>>? Options, CultureInfo? Culture, bool CanAdd) _entriesSource;

    // The option a factory answered, shown for the value it carries until the host's options hold it.
    private SelectOption<TValue>? _addedOption;
    private bool _isAdding;
    private string? _announcement;

    private FormaPopover Popover { get; set; } = default!;

    private FormaVirtualize<ListEntry>? Virtualizer { get; set; }

    private bool IsOpen
        => Popover is { IsOpen: true };

    private bool IsNative
        => !RendererInfo.IsInteractive && IsPostable;

    // A statically rendered field that can't post its value offers nothing to edit.
    private bool IsFieldDisabled
        => Disabled || !RendererInfo.IsInteractive;

    private SelectOption<TValue>? CommittedOption
        => FindCommittedOption();

    // The committed value's label, or the value itself where no option carries it.
    private string CommittedText
        => CommittedOption?.Label
            ?? (!IsValueUnnamed ? string.Empty : ValueLabel is { } valueLabel ? valueLabel(Value!) : Convert.ToString(Value, Context.CultureState.Culture) ?? string.Empty);

    // A value set that no option holds.
    private bool IsValueUnnamed
        => CommittedOption is null && !EqualityComparer<TValue>.Default.Equals(Value, default);

    private string FieldText
        => _isEdited ? _text ?? string.Empty : CommittedText;

    // The value the field renders, which a render never writes back over the reader's typing.
    private string? ShownText
        => _edits.GetShownText(FieldText);

    // A read-only field lists the option it holds alone, so it posts its value and offers no other.
    private IReadOnlyList<SelectOption<TValue>> NativeOptions
        => !ReadOnly ? Options : FindCommittedOption() is { } chosenOption ? [chosenOption] : [];

    private string ListId
        => $"{ElementId}-list";

    private string CommittedIconLabelId
        => $"{ElementId}-icon-label";

    private string? ActiveEntryId
        => IsOpen && _activeIndex >= 0 && _activeIndex < Entries.Count ? GetEntryId(_activeIndex) : null;

    // The active entry stays drawn wherever the list scrolls, so the field always names an element that is there.
    private object[] PinnedKeys
        => _activeIndex >= 0 && _activeIndex < Entries.Count ? [Entries[_activeIndex].Key] : [];

    private IReadOnlyList<ListEntry> Entries
    {
        get
        {
            var source = (FieldText, _isEdited, Options, Context.CultureState.UICulture, OptionFactory is not null);

            if (source != _entriesSource)
            {
                _entriesSource = source;
                _entries = CreateEntries(source.FieldText);
            }

            return _entries;
        }
    }

    /// <inheritdoc />
    protected override bool CanFocus
        => !Disabled && base.CanFocus;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _inputRegistration = new InputRegistration(InputRouter, Context.JS, HandleActionAsync);

        // The edits start from the text the field is first drawn with, so their first sync has nothing of .NET's own to write over
        // what the reader types the moment the field listens.
        _edits = new DomTextEdits(Context.DomEngine, Options is null ? null : FieldText, HandleTextChangedAsync);
        JoinForm(SettleEditsAsync);

        base.OnInitialized();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Options);
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);

        // The options now hold the one added, which they carry from here on.
        if (_addedOption is { } addedOption && Options.Any(option => EqualityComparer<TValue>.Default.Equals(option.Value, addedOption.Value)))
        {
            _addedOption = null;
        }

        if (_isEdited && !EqualityComparer<TValue>.Default.Equals(Value, _valueWhenEdited))
        {
            EndEdit();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsNative || !RendererInfo.IsInteractive)
        {
            return;
        }

        await _edits.SyncAsync(RootElement, FieldText, EditOptions);

        // The keys answer as soon as the field is drawn, before its focus is watched.
        await _inputRegistration.SyncAsync(RootElement, IsOpen ? OpenProfile : _isEdited ? EditedClosedProfile : ClosedProfile);

        // Taken away while its text synced or its keys registered, it has no context left to watch the field through.
        if (IsDisposed)
        {
            return;
        }

        if (firstRender)
        {
            _focusState = await Context.DomWatcher.GetElement(RootElement).Focus.WatchAsync(HandleFocusStateAsync);
        }

        if (_focusState is { } focusState)
        {
            await focusState.SyncAsync(RootElement, FocusOptions);
        }
    }

    // The form reads the field once its text holds every edit, and once that text is resolved as leaving the field resolves it.
    private async Task<bool> SettleEditsAsync()
    {
        var isSettled = await _edits.SettleAsync();

        if (isSettled)
        {
            await InvokeAsync(ResolveEdit);
        }

        return isSettled;
    }

    private SelectOption<TValue>? FindCommittedOption()
        => Options.FirstOrDefault(IsCommitted)
            ?? (_addedOption is { } addedOption && IsCommitted(addedOption) ? addedOption : null);

    private bool IsCommitted(SelectOption<TValue> option)
        => EqualityComparer<TValue>.Default.Equals(option.Value, Value);

    // The options whose label is the text, ignoring case, accents and width and the spaces around it; the ranking's order plays no part.
    private List<SelectOption<TValue>> FindExactOptions(string text)
    {
        var trimmed = text.Trim();
        var compareInfo = Context.CultureState.UICulture.CompareInfo;

        return [.. Options.Where(option => compareInfo.Compare(option.Label.Trim(), trimmed, Loose) == 0)];
    }

    // The options the text ranks, all of them where nothing is typed, then the offer to add the text where no option is named by it exactly.
    private List<ListEntry> CreateEntries(string text)
    {
        var isQuery = _isEdited && !string.IsNullOrWhiteSpace(text);
        var indexed = Options.Select((option, index) => (Option: option, Index: index)).ToList();
        var ranked = isQuery
            ? QueryRanking.Rank(indexed, entry => entry.Option.Label, null, text, Context.CultureState.UICulture)
            : indexed;
        var entries = ranked.Select((entry, place) => new ListEntry(place, entry.Index, entry.Option, null)).ToList();

        if (isQuery && OptionFactory is not null && FindExactOptions(text).Count == 0)
        {
            entries.Add(new ListEntry(entries.Count, -1, null, text.Trim()));
        }

        return entries;
    }

    private string GetEntryId(int index)
        => $"{ElementId}-option-{index}";

    private string GetEntryIconLabelId(int index)
        => $"{ElementId}-option-{index}-icon-label";

    private string GetEntryClass(ListEntry entry)
        => new CssClassBuilder()
            .Add("forma-combo-box-option")
            .Add("forma-combo-box-option-active", entry.Index == _activeIndex)
            .Add("forma-combo-box-option-current", entry.Option is { } option && IsCommitted(option))
            .Add("forma-combo-box-option-disabled", entry.Option is { IsDisabled: true } || (entry.AddsText is not null && _isAdding))
            .Add("forma-combo-box-option-add", entry.AddsText is not null)
            .ToStringOrNull()!;

    // The browser's own list draws text alone, so an option's text carries what its icon says.
    private static string FormatNativeOptionText(SelectOption<TValue> option)
        => option.IconLabel is { } iconLabel ? $"{option.Label}, {iconLabel}" : option.Label;

    private static bool CanTake(ListEntry entry)
        => entry.Option is not { IsDisabled: true };

    // The first entry from start, stepping by step, that can be taken; -1 where none can.
    private int FindTakeableIndex(int start, int step)
    {
        var entries = Entries;

        for (var index = start; index >= 0 && index < entries.Count; index += step)
        {
            if (CanTake(entries[index]))
            {
                return index;
            }
        }

        return -1;
    }

    private async Task HandleTextChangedAsync(TextChange change)
    {
        if (change.IsFaulted != _isTextFaulted)
        {
            _isTextFaulted = change.IsFaulted;
            ReportTextFault(change.IsFaulted, L["Your text didn't arrive in full. Edit it to try again."]);
        }

        if (ReadOnly || IsFieldDisabled)
        {
            return;
        }

        if (change.Text != FieldText)
        {
            if (!_isEdited)
            {
                _isEdited = true;
                _valueWhenEdited = Value;
            }

            _text = change.Text;
            _activeIndex = FindTakeableIndex(0, 1);
            Announce();

            if (!IsOpen && !change.IsCommit && !_isOutside)
            {
                await Popover.OpenAsync(RootElement);
            }
        }

        // A commit made once the focus is known to have gone resolves the text as leaving does; one made while the focus is within, or
        // before the focus is first reported, as one on Enter can be, leaves the text to the action that follows it.
        if (change.IsCommit && _isOutside)
        {
            if (IsOpen)
            {
                await Popover.CloseAsync();
            }

            ResolveEdit();
        }

        await InvokeAsync(StateHasChanged);
    }

    // Leaving the field closes the list and resolves the text once every edit the field sent before has arrived; a press on the list or
    // the button keeps the focus, so only a move elsewhere leaves, and a first report finding the focus gone from an edited field leaves.
    private Task HandleFocusStateAsync(FocusState state)
        => InvokeAsync(async () =>
        {
            var hasLeft = !state.IsWithin && (_isWithin || (!_isOutside && _isEdited));
            _isWithin = state.IsWithin;
            _isOutside = !state.IsWithin;

            if (!hasLeft)
            {
                return;
            }

            var isSettled = await _edits.SettleAsync();

            // Back in the field while its edits arrived, the reader is still editing.
            if (IsDisposed || _isWithin)
            {
                return;
            }

            if (IsOpen)
            {
                await Popover.CloseAsync();
            }

            if (isSettled)
            {
                ResolveEdit();
            }

            StateHasChanged();
        });

    // Text naming exactly one option that can be chosen commits it, no text clears a value that can be empty, and any other text gives
    // way to the committed value's label; an addition in progress settles the field itself.
    private void ResolveEdit()
    {
        if (!_isEdited || _isAdding)
        {
            return;
        }

        var text = _text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text) && CanBeEmpty)
        {
            Commit(default!);
            return;
        }

        var exactOptions = FindExactOptions(text);

        if (exactOptions is [{ IsDisabled: false } exactOption])
        {
            Commit(exactOption.Value);
            return;
        }

        EndEdit();
    }

    private void Commit(TValue value)
    {
        EndEdit();
        CurrentValue = value;
    }

    private void EndEdit()
    {
        _isEdited = false;
        _text = null;
        _activeIndex = -1;
        _announcement = null;
    }

    private void Announce()
        => _announcement = Entries.Count(entry => entry.Option is not null) is var count and > 0
            ? L["[$ plural; one: # option; other: # options; $]", count]
            : L["No matches"];

    private async Task HandleActionAsync(string action, InputInfo input)
    {
        if (IsFieldDisabled || ReadOnly)
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
            case ChooseAction when IsOpen && _activeIndex >= 0 && _activeIndex < Entries.Count:
                await TakeEntryAsync(Entries[_activeIndex]);
                break;
            case RestoreAction when !IsOpen && _isEdited:
                EndEdit();
                StateHasChanged();
                break;
            default:
                break;
        }
    }

    private Task ToggleAsync()
        => IsOpen ? Popover.CloseAsync() : OpenAsync(isForward: true);

    // The list opens on the committed option, or at the end the key moves from where the field holds none.
    private Task OpenAsync(bool isForward)
    {
        var entries = Entries;
        var committedIndex = _isEdited ? -1 : entries.ToList().FindIndex(entry => entry.Option is { } option && IsCommitted(option));
        _activeIndex = committedIndex >= 0
            ? committedIndex
            : isForward ? FindTakeableIndex(0, 1) : FindTakeableIndex(entries.Count - 1, -1);

        return Popover.OpenAsync(RootElement);
    }

    // Moves past the entries that can't be taken, staying at either end.
    private async Task MoveAsync(int step)
    {
        var next = FindTakeableIndex(_activeIndex + step, step);

        if (next < 0)
        {
            return;
        }

        _activeIndex = next;
        StateHasChanged();

        if (Virtualizer is { } virtualizer)
        {
            await virtualizer.ScrollToIndexAsync(_activeIndex);
        }
    }

    private async Task TakeEntryAsync(ListEntry entry)
    {
        if (entry.Option is { IsDisabled: false } option)
        {
            Commit(option.Value);
            await Popover.CloseAsync();
        }
        else if (entry.AddsText is { } text)
        {
            await AddAsync(text);
        }
    }

    // The factory creates the option, and the field alone commits its value; a refused text stays in the field with the list open.
    private async Task AddAsync(string text)
    {
        if (_isAdding || OptionFactory is not { } optionFactory)
        {
            return;
        }

        _isAdding = true;
        StateHasChanged();

        try
        {
            var option = await optionFactory(text);

            if (IsDisposed)
            {
                return;
            }

            if (option is null)
            {
                _announcement = L["\"{Text}\" wasn't added.", text];
            }
            else
            {
                _addedOption = option;
                Commit(option.Value);
                _announcement = L["\"{Text}\" was added.", option.Label];
                await Popover.CloseAsync();
            }
        }
        finally
        {
            _isAdding = false;
        }

        if (!IsDisposed)
        {
            StateHasChanged();
        }
    }

    private void HandleOpenChanged(bool isOpen)
    {
        if (!isOpen)
        {
            _activeIndex = -1;
        }
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-field-input")
            .Add("forma-combo-box-input");

    /// <inheritdoc />
    protected override void BuildDescribedBy(ICollection<string> ids)
    {
        if (!IsNative && !_isEdited && FindCommittedOption() is { IconLabel: not null })
        {
            ids.Add(CommittedIconLabelId);
        }
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _edits.DisposeAsync();

        if (_focusState is { } focusState)
        {
            await focusState.DisposeAsync();
        }

        await _inputRegistration.DisposeAsync();
        await base.DisposeAsyncCore();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(
        string? value,
        out TValue result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        // A choice sets the value directly; the current value, which may be empty, stands in for the result.
        result = Value!;
        validationErrorMessage = null;
        return true;
    }

    // An entry of the list: its place, the option it offers and that option's place among the host's, or the text it offers to add.
    private sealed record ListEntry(int Index, int OptionIndex, SelectOption<TValue>? Option, string? AddsText)
    {
        public object Key
            => Option is null ? -1 : OptionIndex;
    }
}
