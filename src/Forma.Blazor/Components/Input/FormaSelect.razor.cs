namespace Forma.Blazor.Components.Input;

using System.Diagnostics.CodeAnalysis;

using Forma.Blazor.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

/// <summary>A themed single-select: a trigger button opening an anchored, keyboard-navigable option list.</summary>
/// <remarks>Bind <typeparamref name="TValue"/> to a nullable type (<c>int?</c>, <c>string</c>, <c>MyEnum?</c>) to allow no selection.</remarks>
public sealed partial class FormaSelect<TValue> : InputBase<TValue>, IAsyncDisposable
{
    private const string PlacementModulePath = "./_content/Forma.Blazor/scripts/AnchoredPopup.js";
    private const string KeysModulePath = "./_content/Forma.Blazor/Components/Input/FormaSelect.razor.js";

    /// <summary>The options offered in the list.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<SelectOption<TValue>> Options { get; set; } = default!;

    /// <summary>Optional label displayed above the component.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Text shown in the trigger when nothing is selected.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Disables user interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    [Inject] private IStringLocalizer<FormaSelect> L { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private readonly string _labelId = $"sel-label-{Guid.NewGuid():N}";
    private readonly string _triggerTextId = $"sel-trigger-{Guid.NewGuid():N}";
    private readonly string _popupId = $"sel-popup-{Guid.NewGuid():N}";

    private bool _isOpen;
    private bool _placementAttached;
    private int _focusedIndex = -1;
    private ElementReference _anchorElement;
    private ElementReference _triggerElement;
    private ElementReference _popupElement;
    private ElementReference[] _optionElements = [];
    private JsModule _placementModule = default!;
    private JsModule _keysModule = default!;
    private IJSObjectReference? _placement;

    // Both the caller's label and the trigger's own text, so the component announces
    // "Culture, Spanish" rather than losing one to the other.
    private string? LabelledBy
        => Label is null ? null : $"{_labelId} {_triggerTextId}";

    private string TriggerText
        => FindSelectedOption()?.Label ?? Placeholder ?? L["Select an option"];

    protected override void OnInitialized()
    {
        _placementModule = new JsModule(JS, PlacementModulePath);
        _keysModule = new JsModule(JS, KeysModulePath);
    }

    protected override void OnParametersSet()
    {
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
        await CloseAndFocusTriggerAsync();
    }

    private void Toggle()
    {
        if (Disabled)
        {
            return;
        }

        if (_isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    // Opens focused on the current selection, the way a native <select> does, rather than
    // always at the first option.
    private void Open()
    {
        _isOpen = true;

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
    }

    private void Close()
        => _isOpen = false;

    private async Task CloseAndFocusTriggerAsync()
    {
        Close();
        await _triggerElement.FocusAsync();
    }

    private void HandleTriggerKeyDown(KeyboardEventArgs args)
    {
        if (_isOpen || Disabled)
        {
            return;
        }

        switch (args.Key)
        {
            case "ArrowDown":
            case "ArrowUp":
                Open();
                break;
        }
    }

    private async Task HandlePopupKeyDownAsync(KeyboardEventArgs args)
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
            case "Escape":
                await CloseAndFocusTriggerAsync();
                break;
            case "Tab":
                Close();
                break;
        }
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

    // The popup is rebuilt each time it opens, so each open gets its own placement instance
    // bound to that element rather than reusing a stale one.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_isOpen == _placementAttached)
        {
            return;
        }

        try
        {
            if (_isOpen)
            {
                _placement = await _placementModule
                    .InvokeAsync<IJSObjectReference>("AnchoredPopup.create", _popupElement, _anchorElement);

                await _placement.InvokeVoidAsync("open");
                await _keysModule.InvokeVoidAsync("attachNavigationKeys", _popupElement);
                await FocusIndexAsync(_focusedIndex);
            }
            else
            {
                await ReleasePlacementAsync();
            }

            _placementAttached = _isOpen;
        }
        catch (JSDisconnectedException)
        {
            // Circuit disconnected — safe to ignore.
        }
        catch (JSException)
        {
            // Interop failed — safe to ignore.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await ReleasePlacementAsync();
        }
        catch (JSDisconnectedException)
        {
            // Circuit disconnected — safe to ignore.
        }
        catch (JSException)
        {
            // Module never loaded — nothing to release.
        }

        await _placementModule.DisposeAsync();
        await _keysModule.DisposeAsync();
    }

    private async ValueTask ReleasePlacementAsync()
    {
        if (_placement is null)
        {
            return;
        }

        var placement = _placement;
        _placement = null;

        await placement.InvokeVoidAsync("close");
        await placement.DisposeAsync();
    }

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
// IStringLocalizer<T> keys resources off T's FullName, which for an open generic type
// includes its type arguments and would never match the embedded resource name.
internal abstract class FormaSelect;
