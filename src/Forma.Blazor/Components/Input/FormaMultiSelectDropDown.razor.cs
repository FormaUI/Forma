namespace Forma.Blazor.Components.Input;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Blazor.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

/// <summary>A multi-select component whose chosen values render as removable tags above a searchable checkbox-list popup.</summary>
public sealed partial class FormaMultiSelectDropDown<TValue> : InputBase<List<TValue>>, IAsyncDisposable
    where TValue : notnull
{
    private const string ModulePath = "./_content/Forma.Blazor/scripts/AnchoredPopup.js";

    /// <summary>The options offered in the dropdown.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<SelectOption<TValue>> Options { get; set; } = default!;

    /// <summary>Optional label displayed above the component.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Text shown in the trigger when nothing is selected.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Shows an "add new" action in the dropdown, for an option not yet in the list.</summary>
    [Parameter] public bool AllowAdd { get; set; }

    /// <summary>Raised with the current search text when the "add new" action is used.</summary>
    [Parameter] public EventCallback<string> OnAddNew { get; set; }

    /// <summary>Disables user interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    [Inject] private IStringLocalizer<FormaMultiSelectDropDown> L { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private readonly string _labelId = $"ms-label-{Guid.NewGuid():N}";
    private readonly string _triggerTextId = $"ms-trigger-{Guid.NewGuid():N}";
    private readonly string _popupId = $"ms-popup-{Guid.NewGuid():N}";

    private bool _isOpen;
    private bool _placementAttached;
    private string _searchText = "";
    private ElementReference _anchorElement;
    private ElementReference _triggerElement;
    private ElementReference _searchElement;
    private ElementReference _popupElement;
    private JsModule _module = default!;
    private IJSObjectReference? _placement;

    private List<TValue> SelectedValues
        => Value ?? [];

    // Both the caller's label and the trigger's own text, so the component announces
    // "Categories, 2 selected" rather than losing one to the other.
    private string? LabelledBy
        => Label is null ? null : $"{_labelId} {_triggerTextId}";

    private IReadOnlyList<SelectOption<TValue>> FilteredOptions
        => string.IsNullOrWhiteSpace(_searchText)
            ? Options
            : [.. Options.Where(o => o.Label.Contains(_searchText, StringComparison.CurrentCultureIgnoreCase))];

    private bool CanAddSearchText
        => AllowAdd
        && !string.IsNullOrWhiteSpace(_searchText)
        && !Options.Any(o => CultureInfo.CurrentCulture.CompareInfo
            .Compare(o.Label, _searchText.Trim(), CompareOptions.IgnoreCase) == 0);

    private string TriggerText
        => SelectedValues.Count == 0
            ? Placeholder ?? L["SelectOptions"]
            : L["SelectedCount", SelectedValues.Count];

    protected override void OnInitialized()
        => _module = new JsModule(JS, ModulePath);

    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Options);

    private SelectOption<TValue>? FindOption(TValue value)
        => Options.FirstOrDefault(o => EqualityComparer<TValue>.Default.Equals(o.Value, value));

    private bool IsSelected(SelectOption<TValue> option)
        => SelectedValues.Contains(option.Value);

    private void ToggleOption(SelectOption<TValue> option)
    {
        var current = SelectedValues;
        CurrentValue = IsSelected(option)
            ? [.. current.Where(v => !EqualityComparer<TValue>.Default.Equals(v, option.Value))]
            : [.. current, option.Value];
    }

    private void RemoveValue(TValue value)
        => CurrentValue = [.. SelectedValues.Where(v => !EqualityComparer<TValue>.Default.Equals(v, value))];

    private void Toggle()
    {
        if (Disabled)
        {
            return;
        }

        _isOpen = !_isOpen;

        if (_isOpen)
        {
            _searchText = "";
        }
    }

    private void Close()
        => _isOpen = false;

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (!_isOpen || args.Key != "Escape")
        {
            return;
        }

        Close();
        await _triggerElement.FocusAsync();
    }

    private async Task HandleAddNewAsync()
    {
        await OnAddNew.InvokeAsync(_searchText.Trim());
        _searchText = "";
    }

    // The popup element is rebuilt each time it opens, so each open gets its own placement
    // instance bound to that element rather than reusing a stale one.
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
                _placement = await _module
                    .InvokeAsync<IJSObjectReference>("AnchoredPopup.create", _popupElement, _anchorElement);

                await _placement.InvokeVoidAsync("open");
                await _searchElement.FocusAsync();
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

        await _module.DisposeAsync();
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
        out List<TValue> result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = SelectedValues;
        validationErrorMessage = null;
        return true;
    }
}

// Non-generic marker sharing FormaMultiSelectDropDown's resx across every closed instantiation —
// IStringLocalizer<T> keys resources off T's FullName, which for an open generic type
// includes its type arguments and would never match the embedded resource name.
internal abstract class FormaMultiSelectDropDown;
