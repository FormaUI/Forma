namespace Forma.UI.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Framework.Localization;
using Forma.UI.Blazor.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>A multi-select component whose chosen values render as removable tags above a searchable checkbox-list popup.</summary>
public sealed partial class FormaMultiSelect<TValue> : InputBase<List<TValue>>, IAsyncDisposable
    where TValue : notnull
{
    /// <summary>The options offered in the dropdown.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<SelectOption<TValue>> Options { get; set; } = default!;

    /// <summary>The field's label.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Shows the required marker beside the label.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Text shown in the trigger when nothing is selected.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Shows an "add new" action in the dropdown, for an option not yet in the list.</summary>
    [Parameter] public bool AllowAdd { get; set; }

    /// <summary>Raised with the current search text when the "add new" action is used.</summary>
    [Parameter] public EventCallback<string> OnAddNew { get; set; }

    /// <summary>Disables user interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    [Inject] private IStringLocale<FormaMultiSelect> L { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private readonly string _triggerId = $"ms-trigger-{Guid.NewGuid():N}";
    private readonly string _popupId = $"ms-popup-{Guid.NewGuid():N}";

    private bool _isOpen;
    private string _searchText = "";
    private ElementReference _anchorElement;
    private ElementReference _triggerElement;
    private ElementReference _searchElement;
    private ElementReference _popupElement;
    private AnchoredPopup _popup = default!;

    private List<TValue> SelectedValues
        => Value ?? [];

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
    {
        get
        {
            var selected = SelectedValues.Count;

            return selected == 0
                ? Placeholder ?? L["Select Options"]
                : L[$"{selected} selected"];
        }
    }

    protected override void OnInitialized()
        => _popup = new AnchoredPopup(JS);

    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        ArgumentNullException.ThrowIfNull(Options);
    }

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
        => await _popup.SyncAsync(_isOpen, _popupElement, _anchorElement, () => _searchElement.FocusAsync());

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await _popup.DisposeAsync();

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

// Non-generic marker sharing FormaMultiSelect's translation files across every closed instantiation —
// IStringLocale<T> keys resources off T's FullName, which for an open generic type
// includes its type arguments and would never match the embedded resource name.
internal abstract class FormaMultiSelect;
