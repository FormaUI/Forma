namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;

using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Components.Interaction;
using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A single-select field: a trigger button opening an anchored, keyboard-navigable option list.</summary>
/// <typeparam name="TValue">The type of the selected value, where a nullable type holds null for no selection.</typeparam>
public sealed partial class FormaSelect<TValue> : FormaInputBase<TValue>
{
    private const string OpenAction = "select.open";
    private const string NextAction = "select.next";
    private const string PreviousAction = "select.previous";
    private const string FirstAction = "select.first";
    private const string LastAction = "select.last";
    private const string CloseAction = "select.close";
    private const string FindAction = "select.find";

    private static readonly InputProfile TriggerProfile = new()
    {
        InputRoutes =
        [
            new InputRoute(OpenAction, new KeyBinding("ArrowDown"), new KeyBinding("ArrowUp")),

            // A label typed on the closed list opens it on the option it names, choosing nothing.
            new InputRoute(FindAction, new TypingBinding()),
        ],
    };

    private static readonly IReadOnlyList<InputRoute> ListRoutes =
    [
        new InputRoute(NextAction, new KeyBinding("ArrowDown")),
        new InputRoute(PreviousAction, new KeyBinding("ArrowUp")),
        new InputRoute(FirstAction, new KeyBinding("Home")),
        new InputRoute(LastAction, new KeyBinding("End")),
        new InputRoute(FindAction, new TypingBinding()),

        // Tab closes the list and still moves the focus on, so its default is kept.
        new InputRoute(CloseAction, new KeyBinding("Tab")) { PreventDefault = false, Propagation = InputPropagation.Continue },
    ];

    // Whether an option's value posts as text the server's form binding reads back, which a statically rendered page's own list needs.
    private static readonly bool IsPostable = FormValue.IsPostable<TValue>();

    /// <summary>The options offered in the list.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<SelectOption<TValue>> Options { get; set; } = default!;

    /// <summary>The field's label.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Whether the field is marked as required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Text shown in the trigger when nothing is selected.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Whether the field is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private int _focusedIndex = -1;
    private ElementReference _anchorElement;
    private ElementReference[] _optionElements = [];
    private FormaPopover Popover { get; set; } = default!;
    private InputRegistration _trigger = default!;
    private readonly TypeAhead _typeAhead = new();

    private string PopoverId
        => $"{ElementId}-popover";

    private string SelectedIconLabelId
        => $"{ElementId}-icon-label";

    // A select a statically rendered form posts chooses in the browser's own list.
    private bool IsNative
        => !RendererInfo.IsInteractive && IsPostable;

    private bool IsOpen
        => Popover is { IsOpen: true };

    private bool IsTriggerDisabled
        => Disabled || !RendererInfo.IsInteractive;

    private string TriggerText
        => FindSelectedOption()?.Label ?? Placeholder ?? L["Select an Option"];

    /// <inheritdoc />
    protected override bool CanFocus
        => !Disabled && base.CanFocus;

    /// <inheritdoc />
    protected override void OnInitialized()
        => _trigger = new InputRegistration(InputRouter, HandleActionAsync);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        ArgumentNullException.ThrowIfNull(Options);

        if (_optionElements.Length != Options.Count)
        {
            _optionElements = new ElementReference[Options.Count];
        }
    }

    // The trigger's keys route on the trigger, which is always in the document; the list's keys route on the popover.
    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
        => _trigger.SyncAsync(RootElement, TriggerProfile);

    private Task HandleActionAsync(string action, InputInfo input)
    {
        if (Disabled)
        {
            return Task.CompletedTask;
        }

        return action switch
        {
            OpenAction when !IsOpen => OpenAsync(),
            NextAction => MoveFocusAsync(1),
            PreviousAction => MoveFocusAsync(-1),
            FirstAction => FocusIndexAsync(FindEnabledIndex(0, 1)),
            LastAction => FocusIndexAsync(FindEnabledIndex(Options.Count - 1, -1)),
            CloseAction => Popover.CloseAsync(),
            FindAction when input is InputKeyInfo key => FindAsync(key),
            _ => Task.CompletedTask,
        };
    }

    // The option the typed label names takes the focus, the list opening on it where it was closed; nothing is chosen until the reader
    // chooses it.
    private async Task FindAsync(InputKeyInfo key)
    {
        var current = IsOpen ? _focusedIndex : Options.ToList().FindIndex(IsSelected);

        if (_typeAhead.Find(key, [.. Options.Select(option => option.Label)], current, Context.CultureState.Culture, candidate => !Options[candidate].IsDisabled) is not { } index)
        {
            return;
        }

        if (IsOpen)
        {
            await FocusIndexAsync(index);
        }
        else
        {
            _focusedIndex = index;
            await Popover.OpenAsync(RootElement);
        }
    }

    private SelectOption<TValue>? FindSelectedOption()
        => Options.FirstOrDefault(IsSelected);

    private bool IsSelected(SelectOption<TValue> option)
        => EqualityComparer<TValue>.Default.Equals(option.Value, Value);

    private string GetOptionIconLabelId(int index)
        => $"{ElementId}-option-{index}-icon-label";

    private static string GetOptionClass(SelectOption<TValue> option, bool isSelected)
        => new CssClassBuilder()
            .Add("forma-select-option")
            .Add("selected", isSelected)
            .Add("disabled", option.IsDisabled)
            .ToStringOrNull()!;

    // The browser's own list draws text alone, so an option's text carries what its icon says.
    private static string FormatNativeOptionText(SelectOption<TValue> option)
        => option.IconLabel is { } iconLabel ? $"{option.Label}, {iconLabel}" : option.Label;

    private async Task SelectAsync(SelectOption<TValue> option)
    {
        if (option.IsDisabled)
        {
            return;
        }

        CurrentValue = option.Value;
        await Popover.CloseAsync();
    }

    private async Task ToggleAsync()
    {
        if (Disabled)
        {
            return;
        }

        if (IsOpen)
        {
            await Popover.CloseAsync();
        }
        else
        {
            await OpenAsync();
        }
    }

    // Opens focused on the current selection, or on the first option.
    private async Task OpenAsync()
    {
        var selectedIndex = -1;
        for (var i = 0; i < Options.Count; i++)
        {
            if (IsSelected(Options[i]))
            {
                selectedIndex = i;
                break;
            }
        }

        _focusedIndex = selectedIndex >= 0 ? selectedIndex : FindEnabledIndex(0, 1);
        await Popover.OpenAsync(RootElement);
    }

    private Task HandleListActionAsync(InputAction action)
        => HandleActionAsync(action.Action, action.Input);

    private Task HandleOpenedAsync()
        => FocusIndexAsync(_focusedIndex);

    // Moves round the list past the options that can't be chosen.
    private Task MoveFocusAsync(int delta)
    {
        if (Options.Count == 0)
        {
            return Task.CompletedTask;
        }

        return FocusIndexAsync(FindEnabledIndex(((_focusedIndex + delta) % Options.Count + Options.Count) % Options.Count, delta, wraps: true));
    }

    // The first option from start, stepping by step, that can be chosen; -1 where none can.
    private int FindEnabledIndex(int start, int step, bool wraps = false)
    {
        for (var count = 0; count < Options.Count; count++)
        {
            var index = wraps ? ((start + step * count) % Options.Count + Options.Count) % Options.Count : start + step * count;

            if (index < 0 || index >= Options.Count)
            {
                return -1;
            }

            if (!Options[index].IsDisabled)
            {
                return index;
            }
        }

        return -1;
    }

    private async Task FocusIndexAsync(int index)
    {
        if (index < 0 || index >= Options.Count)
        {
            return;
        }

        _focusedIndex = index;
        await _optionElements[_focusedIndex].FocusAsync();
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-field-input")
            .Add("forma-select-trigger");

    /// <inheritdoc />
    protected override void BuildDescribedBy(ICollection<string> ids)
    {
        if (!IsNative && FindSelectedOption() is { IconLabel: not null })
        {
            ids.Add(SelectedIconLabelId);
        }
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _trigger.DisposeAsync();
        await base.DisposeAsyncCore();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(
        string? value,
        out TValue result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        // Selection sets CurrentValue directly; the current value, which may be null, stands in for the result.
        result = Value!;
        validationErrorMessage = null;
        return true;
    }
}
