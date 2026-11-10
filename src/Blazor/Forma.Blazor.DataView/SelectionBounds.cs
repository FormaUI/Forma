namespace Forma.Blazor.Components.DataView;

/// <summary>The minimum and maximum item-count rules for a <see cref="FormaDataView{TItem}"/> selection.</summary>
internal readonly record struct SelectionBounds(int? Minimum, int? Maximum)
{
    /// <summary>Throws if either bound is negative or the minimum exceeds the maximum.</summary>
    public void Validate()
    {
        if (Minimum is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Minimum), Minimum, "Minimum selection cannot be negative.");
        }

        if (Maximum is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Maximum), Maximum, "Maximum selection cannot be negative.");
        }

        if (Minimum is not null && Maximum is not null && Minimum > Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(Maximum), Maximum, $"{nameof(Maximum)} cannot be less than {nameof(Minimum)}.");
        }
    }

    /// <summary>Whether the selection holds one item at most, so a new choice replaces the last.</summary>
    public bool IsSingle
        => Maximum == 1;

    /// <summary>Whether one more item can be selected on top of <paramref name="currentCount"/>.</summary>
    public bool CanAdd(int currentCount)
        => Maximum is null || currentCount < Maximum.Value;

    /// <summary>Whether one item can be removed from <paramref name="currentCount"/> without dropping below the floor.</summary>
    public bool CanRemove(int currentCount)
        => Minimum is null || currentCount > Minimum.Value;

    /// <summary>Whether <paramref name="totalCount"/> items all fit within the maximum.</summary>
    public bool CanSelectAll(int totalCount)
        => Maximum is null || totalCount <= Maximum.Value;
}
