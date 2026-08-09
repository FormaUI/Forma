namespace Forma.UI.Blazor.Tests.Globalization;

using System.Globalization;

using Forma.Tests.Shared;

public sealed class CultureFixtureTests
{
    [Fact]
    public void Should_switch_then_restore_both_cultures()
    {
        // Arrange
        var culture = CultureInfo.CurrentCulture;
        var uiCulture = CultureInfo.CurrentUICulture;

        // Act
        var fixture = new CultureFixture("he-IL");
        var switchedCulture = CultureInfo.CurrentCulture;
        var switchedUiCulture = CultureInfo.CurrentUICulture;
        fixture.Dispose();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("he-IL", switchedCulture.Name);
            Assert.Equal("he-IL", switchedUiCulture.Name);
            Assert.Equal(culture, CultureInfo.CurrentCulture);
            Assert.Equal(uiCulture, CultureInfo.CurrentUICulture);
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_reject_a_blank_culture_name(string? name)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => new CultureFixture(name!));
    }
}
