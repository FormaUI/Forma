namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;

using Forma.Blazor.Components.Feedback;
using Forma.Framework.Common.Interop.Behaviours;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>A themed single-select: a trigger button opening an anchored, keyboard-navigable option list.</summary>
/// <remarks>Bind <typeparamref name="TValue"/> to a nullable type (<c>int?</c>, <c>string</c>, <c>MyEnum?</c>) to allow no selection.</remarks>
public sealed partial class FormaSelect<TValue> : InputBase<TValue>, IAsyncDisposable
{
    /// <summary>The options offered in the list.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<SelectOption<TValue>> Options { get; set; } = default!;

    /// <summary>The field's label.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Shows the required marker beside the label.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Text shown in the trigger when nothing is selected.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Disables user interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    [Inject] private IStringLocale<FormaSelect> L { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private readonly string _triggerId = $"sel-trigger-{Guid.NewGuid():N}";
    private readonly string _popoverId = $"sel-popover-{Guid.NewGuid():N}";

    private int _focusedIndex = -1;
    private ElementReference _anchorElement;
    private ElementReference _triggerElement;
    private ElementReference[] _optionElements = [];
    private FormaPopover Popover { get; set; } = default!;
    private NavigationGuard _keyGuard = default!;

    private bool IsOpen
        => Popover is { IsOpen: true };

    private string TriggerText
        => FindSelectedOption()?.Label ?? Placeholder ?? L["Select an Option"];

    /// <inheritdoc />
    protected override void OnInitialized()
        => _keyGuard = new NavigationGuard(JS);

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

    // Opens focused on the current selection, the way a native <select> does, rather than
    // always at the first option.
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
        await Popover.OpenAsync(_triggerElement);
    }

    private async Task HandleTriggerKeyDownAsync(KeyboardEventArgs args)
    {
        if (IsOpen || Disabled)
        {
            return;
        }

        switch (args.Key)
        {
            case "ArrowDown":
            case "ArrowUp":
                await OpenAsync();
                break;
        }
    }

    private async Task HandlePopoverKeyDownAsync(KeyboardEventArgs args)
    {
        switch (args.Key)
        {
            case "ArrowDown":
                await MoveFocusAsync(1);
                break;
            case "ArrowUp":
                await MoveFocusAsync(-1);
                break;
            case "Home":
                await FocusIndexAsync(0);
                break;
            case "End":
                await FocusIndexAsync(Options.Count - 1);
                break;
            case "Tab":
                await Popover.CloseAsync();
                break;
        }
    }

    private async Task HandleOpenedAsync()
    {
        await _keyGuard.AttachAsync(Popover.Element);
        await FocusIndexAsync(_focusedIndex);
    }

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
    public async ValueTask DisposeAsync()
        => await _keyGuard.DisposeAsync();

    /// <inheritdoc />
    protected override bool TryParseValueFromString(
        string? value,
        out TValue result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        // Never actually invoked — CurrentValue is set directly in SelectAsync rather than
        // round-tripped through a string, but the framework's contract requires a non-null
        // result whenever this returns true, which the current selection may not be.
        result = Value!;
        validationErrorMessage = null;
        return true;
    }
}

// Non-generic marker sharing FormaSelect's translation files across every closed instantiation —
// IStringLocale<T> keys resources off T's FullName, which for an open generic type
// includes its type arguments and would never match the embedded resource name.
internal abstract class FormaSelect;
