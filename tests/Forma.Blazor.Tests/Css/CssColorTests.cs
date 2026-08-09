namespace Forma.Blazor.Tests.Css;

using System.Globalization;

using Forma.Blazor.Css;

public sealed class CssColorTests
{
    [Fact]
    public void Should_keep_channels_from_rgb()
    {
        // Act
        var colour = CssColor.FromRgb(98, 105, 118);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(98, colour.Red);
            Assert.Equal(105, colour.Green);
            Assert.Equal(118, colour.Blue);
            Assert.Equal(1d, colour.Alpha);
            Assert.True(colour.IsOpaque);
        });
    }

    [Fact]
    public void Should_keep_channels_from_rgba()
    {
        // Act
        var colour = CssColor.FromRgba(98, 105, 118, 0.12);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(98, colour.Red);
            Assert.Equal(0.12, colour.Alpha);
            Assert.False(colour.IsOpaque);
        });
    }

    [Fact]
    public void Should_unpack_channels_from_hex()
    {
        // Act
        var colour = CssColor.FromHex(0x2c384a);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(0x2c, colour.Red);
            Assert.Equal(0x38, colour.Green);
            Assert.Equal(0x4a, colour.Blue);
            Assert.True(colour.IsOpaque);
        });
    }

    [Fact]
    public void Should_render_hex_regardless_of_how_it_was_created()
    {
        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("#2c384a", CssColor.FromHex(0x2c384a).ToHexString());
            Assert.Equal("#2c384a", CssColor.FromRgb(0x2c, 0x38, 0x4a).ToHexString());
        });
    }

    [Fact]
    public void Should_render_rgba_regardless_of_how_it_was_created()
    {
        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("rgba(98, 105, 118, 0.12)", CssColor.FromRgba(98, 105, 118, 0.12).ToRgbaString());
            Assert.Equal("rgba(44, 56, 74, 1)", CssColor.FromHex(0x2c384a).ToRgbaString());
        });
    }

    [Fact]
    public void Should_prefer_hex_for_an_opaque_colour()
    {
        // Act & Assert
        Assert.Equal("#2c384a", CssColor.FromHex(0x2c384a).ToString());
    }

    [Fact]
    public void Should_prefer_rgba_for_a_translucent_colour()
    {
        // Act & Assert
        Assert.Equal("rgba(98, 105, 118, 0.12)", CssColor.FromRgba(98, 105, 118, 0.12).ToString());
    }

    [Fact]
    public void Should_format_alpha_invariantly()
    {
        // Arrange
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        try
        {
            // Act & Assert
            Assert.Equal("rgba(0, 0, 0, 0.5)", CssColor.FromRgba(0, 0, 0, 0.5).ToRgbaString());
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Should_pad_single_digit_channels()
    {
        // Act & Assert
        Assert.Equal("#010203", CssColor.FromRgb(1, 2, 3).ToHexString());
    }

    [Fact]
    public void Should_compare_by_value()
    {
        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(CssColor.FromHex(0x2c384a), CssColor.FromRgb(0x2c, 0x38, 0x4a));
            Assert.NotEqual(CssColor.FromRgba(0, 0, 0, 1), CssColor.FromRgba(0, 0, 0, 0.5));
        });
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Should_reject_alpha_outside_the_unit_range(double alpha)
    {
        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CssColor.FromRgba(0, 0, 0, alpha));
            Assert.Throws<ArgumentOutOfRangeException>(() => CssColor.FromHex(0x000000, alpha));
        });
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0x1000000)]
    public void Should_reject_hex_outside_24_bits(int value)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => CssColor.FromHex(value));
    }
}
