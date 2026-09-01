namespace Forma.Blazor.Components.Inputs;

/// <summary>A range being picked: the anchored first day, the day under the pointer, and the ordered span between them.</summary>
internal sealed class DateRangeDraft
{
    /// <summary>The first day picked, or null while no range is being drawn.</summary>
    public DateOnly? Anchor { get; private set; }

    /// <summary>The day the pointer last crossed while the range was being drawn.</summary>
    public DateOnly? Hover { get; private set; }

    /// <summary>Whether a first day has been picked.</summary>
    public bool IsAnchored
        => Anchor is not null;

    /// <summary>The anchored day and the hovered day in date order, or null while no range is being drawn.</summary>
    public (DateOnly From, DateOnly To)? Span
    {
        get
        {
            if (Anchor is not { } anchor)
            {
                return null;
            }

            var hover = Hover ?? anchor;

            return anchor <= hover ? (anchor, hover) : (hover, anchor);
        }
    }

    /// <summary>Starts a range at the given day.</summary>
    public void AnchorAt(DateOnly date)
    {
        Anchor = date;
        Hover = date;
    }

    /// <summary>Moves the hovered day, meaning nothing while no range is being drawn.</summary>
    public void HoverAt(DateOnly date)
    {
        if (IsAnchored)
        {
            Hover = date;
        }
    }

    /// <summary>Ends the range at the given day, answering it in date order and clearing the draft.</summary>
    public (DateOnly Start, DateOnly End) CompleteAt(DateOnly date)
    {
        var anchor = Anchor!.Value;

        Clear();

        return anchor <= date ? (anchor, date) : (date, anchor);
    }

    /// <summary>Drops the draft without completing it.</summary>
    public void Clear()
    {
        Anchor = null;
        Hover = null;
    }
}
