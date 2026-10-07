namespace Forma.Framework.Events.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

using Xunit;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_measure_a_resolved_debouncer_on_the_system_clock()
    {
        // Arrange
        using var provider = new ServiceCollection().AddFormaEventServices().BuildServiceProvider();

        // Act
        var debouncer = Assert.IsType<Debouncer>(provider.GetRequiredService<IDebounceable>());

        // Assert
        Assert.Same(TimeProvider.System, debouncer.Clock);
    }

    [Fact]
    public void Should_measure_a_resolved_debouncer_on_the_clock_the_host_registered()
    {
        // Arrange
        var clock = new FakeTimeProvider();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(clock);
        using var provider = services.AddFormaEventServices().BuildServiceProvider();

        // Act
        var debouncer = Assert.IsType<Debouncer>(provider.GetRequiredService<IDebounceable>());

        // Assert
        Assert.Same(clock, debouncer.Clock);
    }
}
