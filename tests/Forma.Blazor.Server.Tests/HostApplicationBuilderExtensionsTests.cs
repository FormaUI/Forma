namespace Forma.Blazor.Server.Tests;

using Forma.Framework.Culture;
using Forma.Framework.Input;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Configuration;
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

    public sealed class Capture
    {
        private const string Setting = "Components:UseCultureFromServer";

        private static HostApplicationBuilder CreateBuilder(bool rendersWebAssembly = true)
        {
            var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());

            if (rendersWebAssembly)
            {
                builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
            }

            return builder;
        }

        private static void SetCapture(HostApplicationBuilder builder, string value)
            => builder.Configuration.AddInMemoryCollection([new(Setting, value)]);

        [Fact]
        public void Should_supply_capture_on_where_the_configuration_names_none()
        {
            // Arrange
            var builder = CreateBuilder();

            // Act
            builder.AddFormaUIServer();

            // Assert
            Assert.Equal("true", builder.Configuration[Setting]);
        }

        [Fact]
        public void Should_leave_the_value_the_host_names_whichever_comes_first()
        {
            // Arrange
            var before = CreateBuilder();
            var after = CreateBuilder();
            SetCapture(before, "false");

            // Act
            before.AddFormaUIServer();
            after.AddFormaUIServer();
            SetCapture(after, "false");

            // Assert
            Assert.Equal("false false", $"{before.Configuration[Setting]} {after.Configuration[Setting]}");
        }

        [Theory]
        [InlineData("false", true)]
        [InlineData("False", false)]
        [InlineData("0", true)]
        public async Task Should_fail_to_start_where_capture_is_off_under_coordinated_pages_whichever_comes_first(string value, bool isSetFirst)
        {
            // Arrange
            var builder = CreateBuilder();

            if (isSetFirst)
            {
                SetCapture(builder, value);
            }

            builder.AddFormaUIServer();

            if (!isSetFirst)
            {
                SetCapture(builder, value);
            }

            using var host = builder.Build();

            // Act
            var exception = await Record.ExceptionAsync(() => host.StartAsync(TestContext.Current.CancellationToken));

            // Assert
            var message = Assert.IsType<InvalidOperationException>(exception).Message;
            Assert.Multiple(
                () => Assert.Contains(Setting, message, StringComparison.Ordinal),
                () => Assert.Contains("coordinated", message, StringComparison.Ordinal),
                () => Assert.Contains($"{nameof(CultureArrangement)}.{nameof(CultureArrangement.Independent)}", message, StringComparison.Ordinal));
        }

        [Fact]
        public async Task Should_start_with_capture_off_where_the_client_s_culture_is_independent()
        {
            // Arrange
            var builder = CreateBuilder();
            SetCapture(builder, "false");
            builder.AddFormaUIServer(options => options.CultureArrangement = CultureArrangement.Independent);
            using var host = builder.Build();

            // Act
            var exception = await Record.ExceptionAsync(() => host.StartAsync(TestContext.Current.CancellationToken));

            // Assert
            Assert.Multiple(
                () => Assert.Null(exception),
                () => Assert.Equal("false", builder.Configuration[Setting]));
            await host.StopAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Should_check_nothing_on_a_server_that_renders_no_webassembly()
        {
            // Arrange
            var builder = CreateBuilder(rendersWebAssembly: false);
            SetCapture(builder, "false");
            builder.AddFormaUIServer();
            using var host = builder.Build();

            // Act
            var exception = await Record.ExceptionAsync(() => host.StartAsync(TestContext.Current.CancellationToken));

            // Assert
            Assert.Null(exception);
            await host.StopAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Should_start_with_the_capture_it_supplies()
        {
            // Arrange
            var builder = CreateBuilder();
            builder.AddFormaUIServer();
            using var host = builder.Build();

            // Act
            var exception = await Record.ExceptionAsync(() => host.StartAsync(TestContext.Current.CancellationToken));

            // Assert
            Assert.Null(exception);
            await host.StopAsync(TestContext.Current.CancellationToken);
        }
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
