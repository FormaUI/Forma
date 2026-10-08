namespace Forma.Framework.Browser.Interop.Focus;

/// <summary>Where the focus goes inside an element.</summary>
public sealed record FocusInteractionOptions
{
    /// <summary>Whether Tab wraps among the element's controls.</summary>
    public bool Wraps { get; init; }

    /// <summary>The selectors of the regions whose controls Tab visits in turn before the rest of the element's.</summary>
    public IReadOnlyList<string> RegionSelectors { get; init; } = [];

    /// <summary>The axis the arrow keys move the element's one Tab stop along among its controls.</summary>
    public FocusOrientation? RovingOrientation { get; init; }

    /// <summary>The attribute marking the element that takes the focus when the one holding it goes away.</summary>
    public string? HeirAttribute { get; init; }

    /// <inheritdoc />
    public bool Equals(FocusInteractionOptions? other)
        => other is not null
            && Wraps == other.Wraps
            && RegionSelectors.SequenceEqual(other.RegionSelectors)
            && RovingOrientation == other.RovingOrientation
            && HeirAttribute == other.HeirAttribute;

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(Wraps, RegionSelectors.Count, RovingOrientation, HeirAttribute);
}
