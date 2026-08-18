namespace Forma.UI.Blazor.Components.Data;

/// <summary>The minimum and maximum row-count rules for a <see cref="FormaDataGrid{TGridItem}"/> selection.</summary>
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

    /// <summary>Whether one more row can be selected on top of <paramref name="currentCount"/>.</summary>
    public bool CanAdd(int currentCount)
        => Maximum is null || currentCount < Maximum.Value;

    /// <summary>Whether one row can be removed from <paramref name="currentCount"/> without dropping below the floor.</summary>
    public bool CanRemove(int currentCount)
        => Minimum is null || currentCount > Minimum.Value;

    /// <summary>Whether <paramref name="totalCount"/> rows all fit within the maximum.</summary>
    public bool CanSelectAll(int totalCount)
        => Maximum is null || totalCount <= Maximum.Value;
}
