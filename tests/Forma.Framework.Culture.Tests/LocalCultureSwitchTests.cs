namespace Forma.Framework.Culture.Tests;

using System.Globalization;

using Forma.Tests.Culture;
using Forma.Tests.Shared;

using Xunit;

public sealed class LocalCultureSwitchTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo Hebrew = CultureInfo.GetCultureInfo("he");

    [Fact]
    public async Task Should_accept_each_switch_at_once_in_a_sequence_of_its_own()
    {
        // Arrange
        using var fixture = new CultureFixture("en");
        var state = new FakeCultureState(English);
        var cultureSwitch = new LocalCultureSwitch(state);

        // Act
        await cultureSwitch.SwitchAsync(German, Hebrew);
        await cultureSwitch.SwitchAsync(Hebrew);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["de-DE/he", "he/he"], state.Applied.Select(accepted => $"{accepted.Culture.Name}/{accepted.UICulture.Name}")),
            () => Assert.Equal([1, 2], state.Applied.Select(accepted => accepted.Version.Number)),
            () => Assert.Single(state.Applied.Select(accepted => accepted.Version.DocumentKey).Distinct()),
            () => Assert.NotEqual(CultureVersion.Initial.DocumentKey, state.Applied[0].Version.DocumentKey));
    }

    [Fact]
    public async Task Should_keep_its_sequence_apart_from_another_switchs()
    {
        // Arrange
        using var fixture = new CultureFixture("en");
        var state = new FakeCultureState(English);

        // Act
        await new LocalCultureSwitch(state).SwitchAsync(German);
        await new LocalCultureSwitch(state).SwitchAsync(Hebrew);

        // Assert
        Assert.NotEqual(state.Applied[0].Version.DocumentKey, state.Applied[1].Version.DocumentKey);
    }

    [Fact]
    public void Should_apply_the_pair_before_it_returns()
    {
        // Arrange
        using var fixture = new CultureFixture("en");
        var state = new FakeCultureState(English);

        // Act
        _ = new LocalCultureSwitch(state).SwitchAsync(Hebrew);

        // Assert
        Assert.Equal(Hebrew, state.UICulture);
    }

    [Fact]
    public async Task Should_reject_a_missing_culture()
    {
        // Arrange
        var cultureSwitch = new LocalCultureSwitch(new FakeCultureState(English));

        // Act
        var exception = await Record.ExceptionAsync(() => cultureSwitch.SwitchAsync(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }
}
