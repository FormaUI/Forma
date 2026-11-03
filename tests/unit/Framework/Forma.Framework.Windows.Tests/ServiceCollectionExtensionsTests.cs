namespace Forma.Framework.Windows.Tests;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_one_manager_per_scope()
    {
        // Arrange
        using var provider = new ServiceCollection().AddFormaWindowServices().BuildServiceProvider();

        // Act
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        // Assert
        Assert.Multiple(
            () => Assert.Same(first.ServiceProvider.GetRequiredService<WindowManager>(), first.ServiceProvider.GetRequiredService<WindowManager>()),
            () => Assert.NotSame(first.ServiceProvider.GetRequiredService<WindowManager>(), second.ServiceProvider.GetRequiredService<WindowManager>()));
    }
}
