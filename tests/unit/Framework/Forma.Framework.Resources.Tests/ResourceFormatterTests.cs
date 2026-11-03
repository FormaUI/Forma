namespace Forma.Framework.Resources.Tests;

using System.Globalization;

using Xunit;

public sealed class ResourceFormatterTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(104857600, "100 MB")]
    [InlineData(1073741824, "1 GB")]
    [InlineData(1099511627776, "1 TB")]
    [InlineData(1649267441664, "1.5 TB")]
    public void Should_write_a_byte_count_in_its_largest_whole_unit(long byteCount, string expected)
    {
        // Act
        var formatted = ResourceFormatter.FromByteCount(byteCount, English);

        // Assert
        Assert.Equal(expected, formatted);
    }

    [Fact]
    public void Should_write_a_byte_count_in_the_culture_given()
    {
        // Act
        var formatted = ResourceFormatter.FromByteCount(1536, CultureInfo.GetCultureInfo("de-DE"));

        // Assert
        Assert.Equal("1,5 KB", formatted);
    }

    [Fact]
    public void Should_reject_a_negative_byte_count()
        => Assert.Throws<ArgumentOutOfRangeException>(() => ResourceFormatter.FromByteCount(-1, English));

    [Theory]
    [InlineData(0, "0 ms")]
    [InlineData(1, "1 ms")]
    [InlineData(420, "420 ms")]
    [InlineData(999, "999 ms")]
    public void Should_write_a_sub_second_duration_in_milliseconds(int milliseconds, string expected)
    {
        // Act
        var formatted = ResourceFormatter.FromDuration(TimeSpan.FromMilliseconds(milliseconds), English);

        // Assert
        Assert.Equal(expected, formatted);
    }

    [Theory]
    [InlineData(1000, "1 s")]
    [InlineData(4200, "4.2 s")]
    [InlineData(59900, "59.9 s")]
    public void Should_write_a_sub_minute_duration_in_seconds(int milliseconds, string expected)
    {
        // Act
        var formatted = ResourceFormatter.FromDuration(TimeSpan.FromMilliseconds(milliseconds), English);

        // Assert
        Assert.Equal(expected, formatted);
    }

    [Theory]
    [InlineData(60, "1:00")]
    [InlineData(83, "1:23")]
    [InlineData(3723, "1:02:03")]
    public void Should_write_a_duration_gaining_hours_only_when_it_runs_that_long(int seconds, string expected)
    {
        // Act
        var formatted = ResourceFormatter.FromDuration(TimeSpan.FromSeconds(seconds), English);

        // Assert
        Assert.Equal(expected, formatted);
    }

    [Fact]
    public void Should_reject_a_negative_duration()
        => Assert.Throws<ArgumentOutOfRangeException>(() => ResourceFormatter.FromDuration(TimeSpan.FromSeconds(-1), English));

    [Fact]
    public void Should_write_a_transfer_rate_over_one_second()
    {
        // Act
        var formatted = ResourceFormatter.FromTransferRate(1536, English);

        // Assert
        Assert.Equal("1.5 KB/s", formatted);
    }

    [Fact]
    public void Should_write_an_unmeasurable_transfer_rate_as_nothing_moving()
    {
        // Act
        var formatted = ResourceFormatter.FromTransferRate(double.PositiveInfinity, English);

        // Assert
        Assert.Equal("0 B/s", formatted);
    }
}
