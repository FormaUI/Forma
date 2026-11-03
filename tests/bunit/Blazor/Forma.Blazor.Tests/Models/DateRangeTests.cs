namespace Forma.Blazor.Tests.Models;

using Forma.Blazor.Models;

public sealed class DateRangeTests
{
    [Fact]
    public void Should_hold_a_first_and_a_last_date()
    {
        // Act
        var range = new DateRange(new DateOnly(2025, 3, 5), new DateOnly(2025, 3, 8));

        // Assert
        Assert.Multiple(
            () => Assert.Equal(new DateOnly(2025, 3, 5), range.Start),
            () => Assert.Equal(new DateOnly(2025, 3, 8), range.End),
            () => Assert.False(range.IsSingleDay));
    }

    [Fact]
    public void Should_be_a_single_day_when_both_dates_are_the_same()
    {
        // Act
        var range = new DateRange(new DateOnly(2025, 3, 5), new DateOnly(2025, 3, 5));

        // Assert
        Assert.True(range.IsSingleDay);
    }

    [Fact]
    public void Should_contain_its_ends_and_the_days_between()
    {
        // Arrange
        var range = new DateRange(new DateOnly(2025, 3, 5), new DateOnly(2025, 3, 8));

        // Act & Assert
        Assert.Multiple(
            () => Assert.True(range.Contains(new DateOnly(2025, 3, 5))),
            () => Assert.True(range.Contains(new DateOnly(2025, 3, 6))),
            () => Assert.True(range.Contains(new DateOnly(2025, 3, 8))),
            () => Assert.False(range.Contains(new DateOnly(2025, 3, 4))),
            () => Assert.False(range.Contains(new DateOnly(2025, 3, 9))));
    }

    [Fact]
    public void Should_compare_by_value()
    {
        // Act & Assert
        Assert.Multiple(
            () => Assert.Equal(new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 2)), new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 2))),
            () => Assert.NotEqual(new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 2)), new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 3))));
    }

    [Fact]
    public void Should_reject_a_last_date_before_the_first()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DateRange(new DateOnly(2025, 3, 8), new DateOnly(2025, 3, 5)));
    }

    [Fact]
    public void Should_deconstruct_into_its_first_and_last_dates()
    {
        // Arrange
        var range = new DateRange(new DateOnly(2025, 3, 5), new DateOnly(2025, 3, 8));

        // Act
        var (start, end) = range;

        // Assert
        Assert.Multiple(
            () => Assert.Equal(new DateOnly(2025, 3, 5), start),
            () => Assert.Equal(new DateOnly(2025, 3, 8), end));
    }

    [Fact]
    public void Should_order_two_dates_given_either_way_round()
    {
        // Act
        var forward = DateRange.Between(new DateOnly(2025, 3, 5), new DateOnly(2025, 3, 8));
        var backward = DateRange.Between(new DateOnly(2025, 3, 8), new DateOnly(2025, 3, 5));

        // Assert
        Assert.Multiple(
            () => Assert.Equal(new DateRange(new DateOnly(2025, 3, 5), new DateOnly(2025, 3, 8)), forward),
            () => Assert.Equal(forward, backward));
    }
}
