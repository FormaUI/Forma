namespace Forma.Blazor.Components.Feedback;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A readout of how much of a limit is used, as a count against the limit and optionally a bar, naming the state once the limit nears.</summary>
/// <remarks>
/// The limit is near once the count reaches <see cref="NearLimitRatio"/> of it, at that share as well as above it, and full once the
/// count reaches the limit, over it once the count passes it; a limit of zero is full from the start. The meter's value stops at the
/// limit, while its text and the count keep the number used.
/// </remarks>
public sealed partial class FormaCapacityIndicator : FormaElementBase
{
    /// <summary>How much of the limit is used.</summary>
    [Parameter, EditorRequired] public int Used { get; set; }

    /// <summary>The most that can be used.</summary>
    [Parameter, EditorRequired] public int Limit { get; set; }

    /// <summary>The label naming what the readout counts.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>A line of supporting text shown under the readout.</summary>
    [Parameter] public string? Note { get; set; }

    /// <summary>Whether a bar shows how much of the limit is used.</summary>
    [Parameter] public bool ShowBar { get; set; }

    /// <summary>The share of the limit at and above which the limit counts as near.</summary>
    [Parameter] public decimal NearLimitRatio { get; set; } = 0.8m;

    private string LabelId
        => $"{ElementId}-label";

    private string NoteId
        => $"{ElementId}-note";

    private CapacityState State
        => Used > Limit ? CapacityState.Over
            : Used == Limit ? CapacityState.Full
            : Used >= Limit * NearLimitRatio ? CapacityState.Near
            : CapacityState.Within;

    private string LabelText
        => Label ?? L["Capacity"];

    private string CountText
        => $"{FormatCount(Used)} / {FormatCount(Limit)}";

    private string? DescribedBy
        => Note is null ? null : NoteId;

    private int ValueNow
        => Math.Min(Used, Limit);

    private string ValueText
        => State switch
        {
            CapacityState.Near => L["{Used} of {Limit}, nearly full", FormatCount(Used), FormatCount(Limit)],
            CapacityState.Full => L["{Used} of {Limit}, full", FormatCount(Used), FormatCount(Limit)],
            CapacityState.Over => L["{Used} of {Limit}, over the limit", FormatCount(Used), FormatCount(Limit)],
            _ => L["{Used} of {Limit}", FormatCount(Used), FormatCount(Limit)],
        };

    private string? StateText
        => State switch
        {
            CapacityState.Near => L["Nearly Full"],
            CapacityState.Full => L["Full"],
            CapacityState.Over => L["Over the Limit"],
            _ => null,
        };

    // The share of the limit used, where a limit of nothing is full.
    private double UsedPercentage
        => Limit == 0 ? 100 : Math.Min(Used, Limit) * 100d / Limit;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(Used);
        ArgumentOutOfRangeException.ThrowIfNegative(Limit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(NearLimitRatio);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(NearLimitRatio, 1m);
    }

    private string FormatCount(int count)
        => count.ToString("N0", Context.CultureState.Culture);

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-capacity-indicator")
            .Add("forma-capacity-indicator-near", State == CapacityState.Near)
            .Add("forma-capacity-indicator-full", State is CapacityState.Full or CapacityState.Over);

    /// <inheritdoc />
    protected override void BuildRootStyle(CssStyleBuilder builder)
        => builder.Add("--forma-capacity-indicator-used-size", CssLength.FromPercent(UsedPercentage).ToString(), ShowBar);

    private enum CapacityState
    {
        Within,
        Near,
        Full,
        Over,
    }
}
