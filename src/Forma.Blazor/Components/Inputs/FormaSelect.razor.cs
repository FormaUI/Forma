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

    private static readonly InputProfile TriggerProfile = new()
    {
        InputRoutes = [new InputRoute(OpenAction, new KeyBinding("ArrowDown"), new KeyBinding("ArrowUp"))],
    };

    private static readonly IReadOnlyList<InputRoute> ListRoutes =
    [
        new InputRoute(NextAction, new KeyBinding("ArrowDown")),
        new InputRoute(PreviousAction, new KeyBinding("ArrowUp")),
        new InputRoute(FirstAction, new KeyBinding("Home")),
        new InputRoute(LastAction, new KeyBinding("End")),

        // Tab closes the list and still moves the focus on, so its default is kept.
        new InputRoute(CloseAction, new KeyBinding("Tab")) { PreventDefault = false, Propagation = InputPropagation.Continue },
    ];

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

    private string PopoverId
        => $"{ElementId}-popover";

    private bool IsOpen
        => Popover is { IsOpen: true };

    private string TriggerText
        => FindSelectedOption()?.Label ?? Placeholder ?? L["Select an Option"];

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
            FirstAction => FocusIndexAsync(0),
            LastAction => FocusIndexAsync(Options.Count - 1),
            CloseAction => Popover.CloseAsync(),
            _ => Task.CompletedTask,
        };
    }

    private SelectOption<TValue>? FindSelectedOption()
        => Options.FirstOrDefault(IsSelected);

    private bool IsSelected(SelectOption<TValue> option)
        => EqualityComparer<TValue>.Default.Equals(option.Value, Value);

    private async Task SelectAsync(SelectOption<TValue> option)
    {
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

        _focusedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        await Popover.OpenAsync(RootElement);
    }

    private Task HandleListActionAsync(InputAction action)
        => HandleActionAsync(action.Action, action.Input);

    private Task HandleOpenedAsync()
        => FocusIndexAsync(_focusedIndex);

    private Task MoveFocusAsync(int delta)
    {
        if (Options.Count == 0)
        {
            return Task.CompletedTask;
        }

        return FocusIndexAsync(((_focusedIndex + delta) % Options.Count + Options.Count) % Options.Count);
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
