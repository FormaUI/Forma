namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;

using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Components.Interaction;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A field for picking several values from a searchable checkbox list, showing the chosen values as removable tags under it.</summary>
public sealed partial class FormaMultiSelect<TValue> : FormaInputBase<List<TValue>>, IDisableable
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

    /// <summary>Why the field is disabled, which it shows as its tooltip.</summary>
    [Parameter] public string? DisabledReason { get; set; }

    // Whether a value posts as text the server's form binding reads back, which a statically rendered page's own list needs.
    private static readonly bool IsPostable = FormValue.IsPostable<TValue>();

    private string _searchText = "";
    private ElementReference _anchorElement;

    // The field an option checkbox names to the input base, bound to nothing.
    private bool OptionSelection { get; }
    private FormaSearchBox SearchBox { get; set; } = default!;
    private FormaPopover Popover { get; set; } = default!;

    private string PopoverId
        => $"{ElementId}-popover";

    bool IDisableable.IsDisabled
        => Disabled;

    string? IDisableable.DisabledReason
        => DisabledReason;

    private bool IsOpen
        => Popover is { IsOpen: true };

    private bool IsTriggerDisabled
        => Disabled || !RendererInfo.IsInteractive;

    private SearchBoxOptions SearchOptions
        => new() { Placeholder = L["Search"].Value };

    private List<TValue> SelectedValues
        => Value ?? [];

    // The options every word typed matches, ignoring case and accents, in the order a reader looks for them, as the combo box ranks its own.
    private IReadOnlyList<SelectOption<TValue>> FilteredOptions
        => QueryRanking.Rank(Options, option => option.Label, null, _searchText, Context.CultureState.UICulture);

    private bool CanAddSearchText
        => AllowAdd
        && !string.IsNullOrWhiteSpace(_searchText)
        && !Options.Any(option => QueryRanking.IsSameName(option.Label, _searchText, Context.CultureState.UICulture));

    private string TriggerText
    {
        get
        {
            var selectedCount = SelectedValues.Count;

            return selectedCount == 0
                ? Placeholder ?? L["Select Options"]
                : L["{Selected} selected", selectedCount];
        }
    }

    /// <inheritdoc />
    protected override bool CanFocus
        => (!Disabled || ShowsDisabledReason) && base.CanFocus;

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

    private string GetOptionIconLabelId(int index)
        => $"{ElementId}-option-{index}-icon-label";

    // The browser's own list draws text alone, so an option's text carries what its icon says.
    private static string FormatNativeOptionText(SelectOption<TValue> option)
        => option.IconLabel is { } iconLabel ? $"{option.Label}, {iconLabel}" : option.Label;

    // An option that can't be chosen is never added, though one already chosen can still be taken off.
    private void ToggleOption(SelectOption<TValue> option)
    {
        if (option.IsDisabled && !IsSelected(option))
        {
            return;
        }

        var currentValues = SelectedValues;
        CurrentValue = IsSelected(option)
            ? [.. currentValues.Where(v => !EqualityComparer<TValue>.Default.Equals(v, option.Value))]
            : [.. currentValues, option.Value];
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
