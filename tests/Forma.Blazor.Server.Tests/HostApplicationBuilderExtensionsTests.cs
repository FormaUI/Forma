namespace Forma.Blazor.Server.Tests;

using Forma.Framework.Culture;
using Forma.Framework.Input;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Hosting;

public sealed class HostApplicationBuilderExtensionsTests
{
    [Fact]
    public void Should_register_the_circuit_halves_beside_what_every_component_needs()
    {
        // Arrange
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());

        // Act
        builder.AddFormaUIServer();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(2, builder.Services.Count(descriptor => descriptor.ServiceType == typeof(CircuitHandler))),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(builder.Services, descriptor => descriptor.ServiceType == typeof(ICultureState)).Lifetime));
    }

    [Fact]
    public void Should_register_the_default_input_policy()
    {
        // Arrange
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());

        // Act
        builder.AddFormaUIServer();

        // Assert
        using var provider = builder.Services.BuildServiceProvider();
        Assert.Same(InputRouterOptions.Default, provider.GetRequiredService<InputRouterOptions>());
    }

    [Fact]
    public void Should_register_the_host_input_policy()
    {
        // Arrange
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        var policy = new InputRouterOptions { Escape = DismissBehavior.Ignore };

        // Act
        builder.AddFormaUIServer(options => options.InputRouting = policy);

        // Assert
        using var provider = builder.Services.BuildServiceProvider();
        Assert.Same(policy, provider.GetRequiredService<InputRouterOptions>());
    }

    [Fact]
    public void Should_return_the_builder_it_was_given()
    {
        // Arrange
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());

        // Act
        var returned = builder.AddFormaUIServer();

        // Assert
        Assert.Same(builder, returned);
    }
}
