namespace Forma.Blazor.Components.Inputs;

using System.Globalization;

using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Models;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A trigger showing a date, with a calendar that opens beneath it for picking one.</summary>
public sealed partial class FormaDateBox : FormaComponentBase
{
    /// <summary>The id of the trigger, which a label names.</summary>
    [Parameter, EditorRequired] public string ElementId { get; set; } = default!;

    /// <summary>The runtime id of the component the box picks for.</summary>
    [Parameter] public Guid OwnerRuntimeId { get; set; }

    /// <summary>The date the trigger shows.</summary>
    [Parameter] public DateOnly? Date { get; set; }

    /// <summary>The earliest selectable date.</summary>
    [Parameter] public DateOnly? MinDate { get; set; }

    /// <summary>The latest selectable date.</summary>
    [Parameter] public DateOnly? MaxDate { get; set; }

    /// <summary>Whether months come from the culture's non-Gregorian calendar, such as the Hebrew calendar for <c>he</c>.</summary>
    [Parameter] public bool UseNonGregorianMonths { get; set; }

    /// <summary>Raised with the date the reader picked.</summary>
    [Parameter] public EventCallback<DateOnly> OnChoose { get; set; }

    /// <summary>Raised when the reader clears the date.</summary>
    [Parameter] public EventCallback OnClear { get; set; }

    /// <summary>Whether the trigger is marked as required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Whether the trigger is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the date is shown without a way to change it.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>The id of the element naming the trigger, read before the date it shows.</summary>
    [Parameter] public string? NameId { get; set; }

    /// <summary>The classes the trigger carries.</summary>
    [Parameter] public string? TriggerClass { get; set; }

    /// <summary>The style the trigger carries.</summary>
    [Parameter] public string? TriggerStyle { get; set; }

    /// <summary>The attributes the trigger carries.</summary>
    [Parameter] public IReadOnlyDictionary<string, object>? TriggerAttributes { get; set; }

    /// <summary>The space-separated element IDs used for the trigger's aria-describedby attribute.</summary>
    [Parameter] public string? TriggerDescribedBy { get; set; }

    private ElementReference _anchorElement;

    /// <summary>The trigger, once rendered.</summary>
    public ElementReference TriggerElement { get; private set; }

    private FormaPopover Popover { get; set; } = default!;

    private bool IsOpen
        => Popover is { IsOpen: true };

    private bool HasValue
        => Date is not null;

    private string? TriggerClassList
        => new CssClassBuilder().Add("forma-date-box-trigger").Add(TriggerClass).ToStringOrNull();

    private string ValueId
        => $"{ElementId}-value";

    private string? LabelledBy
        => NameId is null ? null : $"{NameId} {ValueId}";

    private string DisplayText
        => Date is { } date ? date.ToDateTime(TimeOnly.MinValue).ToString("d", CultureInfo.CurrentCulture) : string.Empty;

    private DateRange? Highlight
        => Date is { } date ? new DateRange(date, date) : null;

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentException.ThrowIfNullOrWhiteSpace(ElementId);

    private async Task ToggleAsync()
    {
        if (IsOpen)
        {
            await Popover.CloseAsync();
            return;
        }

        await Popover.OpenAsync(TriggerElement);
    }

    private async Task ChooseAsync(DateOnly date)
    {
        await OnChoose.InvokeAsync(date);
        await Popover.CloseAsync();
    }

    private async Task ClearAsync()
    {
        await OnClear.InvokeAsync();
        await Popover.CloseAsync();
    }
}
