namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Blazor.Components.Feedback;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A field for picking several values from a searchable checkbox list, showing the chosen values as removable tags under it.</summary>
public sealed partial class FormaMultiSelect<TValue> : FormaInputBase<List<TValue>>
    where TValue : notnull
{
    /// <summary>The options offered in the dropdown.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<SelectOption<TValue>> Options { get; set; } = default!;

    /// <summary>The field's label.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Whether the field is marked as required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Text shown in the trigger when nothing is selected.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Whether the dropdown offers an "add new" action for search text no option matches.</summary>
    [Parameter] public bool AllowAdd { get; set; }

    /// <summary>Raised with the current search text when the "add new" action is used.</summary>
    [Parameter] public EventCallback<string> OnAddNew { get; set; }

    /// <summary>Whether the field is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    private string _searchText = "";
    private ElementReference _anchorElement;

    // The field an option checkbox names to the input base, bound to nothing.
    private bool OptionSelection { get; }
    private FormaSearchBox SearchBox { get; set; } = default!;
    private FormaPopover Popover { get; set; } = default!;

    private string PopoverId
        => $"{ElementId}-popover";

    private bool IsOpen
        => Popover is { IsOpen: true };

    private SearchBoxOptions SearchOptions
        => new() { Placeholder = L["Search"].Value };

    private List<TValue> SelectedValues
        => Value ?? [];

    private IReadOnlyList<SelectOption<TValue>> FilteredOptions
        => string.IsNullOrWhiteSpace(_searchText)
            ? Options
            : [.. Options.Where(o => CultureState.Culture.CompareInfo.IndexOf(o.Label, _searchText, CompareOptions.IgnoreCase) >= 0)];

    private bool CanAddSearchText
        => AllowAdd
        && !string.IsNullOrWhiteSpace(_searchText)
        && !Options.Any(o => CultureState.Culture.CompareInfo
            .Compare(o.Label, _searchText.Trim(), CompareOptions.IgnoreCase) == 0);

    private string TriggerText
    {
        get
        {
            var selected = SelectedValues.Count;

            return selected == 0
                ? Placeholder ?? L["Select Options"]
                : L["{Selected} selected", selected];
        }
    }

    /// <inheritdoc />
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

    private async Task ToggleAsync()
    {
        if (Disabled)
        {
            return;
        }

        if (IsOpen)
        {
            await Popover.CloseAsync();
            return;
        }

        _searchText = "";
        await Popover.OpenAsync(RootElement);
    }

    private async Task HandleAddNewAsync()
    {
        await OnAddNew.InvokeAsync(_searchText.Trim());
        _searchText = "";
    }

    private Task FocusSearchAsync()
        => SearchBox.RootElement.FocusAsync().AsTask();

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-field-input")
            .Add("forma-multi-select-trigger");

    /// <inheritdoc />
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
