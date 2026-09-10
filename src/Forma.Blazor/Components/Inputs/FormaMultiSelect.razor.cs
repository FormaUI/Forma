namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Blazor.Components.Feedback;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A multi-select component whose chosen values render as removable tags above a searchable checkbox-list popover.</summary>
public sealed partial class FormaMultiSelect<TValue> : FormaInputBase<List<TValue>>
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

    private string _searchText = "";
    private ElementReference _anchorElement;

    // The field an option checkbox names to the input base; an option is not a form field and nothing binds it.
    private bool OptionSelection { get; }
    // The popover's search box; the reference focuses it.
    private FormaSearchBox SearchBox { get; set; } = default!;
    private FormaPopover Popover { get; set; } = default!;

    private string ElementId
        => Id ?? $"ms-{RuntimeId:N}";

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
