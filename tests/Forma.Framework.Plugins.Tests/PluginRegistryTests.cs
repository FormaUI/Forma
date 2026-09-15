namespace Forma.Framework.Plugins.Tests;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class PluginRegistryTests
{
    public interface IHost : IPlugin;

    public sealed class Host : IHost
    {
        public string Id => "host";
    }

    public sealed class SecondHost : IHost
    {
        public string Id => "second-host";
    }

    public sealed class ContractBoundPlugin(IHost host) : IPlugin
    {
        public IHost Host { get; } = host;

        public string Id => "contract-bound";
    }

    public sealed class HostBoundPlugin(Host host) : IPlugin
    {
        public Host Host { get; } = host;

        public string Id => "host-bound";
    }

    public sealed class AlsoHostBoundPlugin(Host host) : IPlugin
    {
        public Host Host { get; } = host;

        public string Id => "also-host-bound";
    }

    public sealed class Clock;

    public sealed class ServiceBoundPlugin(Clock clock) : IPlugin
    {
        public Clock Clock { get; } = clock;

        public string Id => "service-bound";
    }

    public sealed class CountOptions
    {
        public int Count { get; set; } = 1;
    }

    public sealed class CountingPlugin(CountOptions? options = null) : IPlugin, IPluginConfigurable<CountOptions>
    {
        public CountOptions Options { get; } = options ?? new CountOptions();

        public string Id => "counting";
    }

    public sealed class NeedyPlugin(Uri address) : IPlugin
    {
        public Uri Address { get; } = address;

        public string Id => "needy";
    }

    public sealed class AmbiguousPlugin : IPlugin
    {
        public AmbiguousPlugin()
        {
        }

        public AmbiguousPlugin(Clock clock)
        {
            Clock = clock;
        }

        public Clock? Clock { get; }

        public string Id => "ambiguous";
    }

    public sealed class ClosablePlugin : IPlugin, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public string Id => "closable";

        public void Dispose()
            => IsDisposed = true;
    }

    private static ServiceProvider CreateServices()
        => new ServiceCollection().BuildServiceProvider();

    public sealed class Activation
    {
        [Fact]
        public void Should_build_the_plugins_in_the_order_they_were_registered()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>()
                .Add<Host>()
                .Add<HostBoundPlugin>();

            // Act
            var plugins = registry.Activate(services).Plugins;

            // Assert
            Assert.Collection(
                plugins,
                plugin => Assert.IsType<Host>(plugin),
                plugin => Assert.IsType<HostBoundPlugin>(plugin));
        }

        [Fact]
        public void Should_hand_every_plugin_the_one_instance_this_owner_built()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>()
                .Add<Host>()
                .Add<HostBoundPlugin>()
                .Add<AlsoHostBoundPlugin>();

            // Act
            var plugins = registry.Activate(services).Plugins;

            // Assert
            var host = plugins.OfType<Host>().Single();
            Assert.Multiple(
                () => Assert.Same(host, plugins.OfType<HostBoundPlugin>().Single().Host),
                () => Assert.Same(host, plugins.OfType<AlsoHostBoundPlugin>().Single().Host));
        }

        [Fact]
        public void Should_build_a_separate_scope_for_every_owner()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>()
                .Add<Host>()
                .Add<HostBoundPlugin>();

            // Act
            var first = registry.Activate(services).Plugins;
            var second = registry.Activate(services).Plugins;

            // Assert
            Assert.Multiple(
                () => Assert.NotSame(first.OfType<Host>().Single(), second.OfType<Host>().Single()),
                () => Assert.NotSame(first.OfType<HostBoundPlugin>().Single(), second.OfType<HostBoundPlugin>().Single()));
        }

        [Fact]
        public void Should_hand_a_plugin_the_owner_it_sits_under_over_one_registered_earlier()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>()
                .Add<Host>()
                .Add<SecondHost>(host => host.Add<ContractBoundPlugin>());

            // Act
            var plugins = registry.Activate(services).Plugins;

            // Assert
            Assert.Same(plugins.OfType<SecondHost>().Single(), plugins.OfType<ContractBoundPlugin>().Single().Host);
        }

        [Fact]
        public void Should_resolve_what_no_plugin_supplies_from_the_host()
        {
            // Arrange
            var clock = new Clock();
            using var services = new ServiceCollection().AddSingleton(clock).BuildServiceProvider();
            var registry = new PluginRegistry<IPlugin>().Add<ServiceBoundPlugin>();

            // Act
            var plugins = registry.Activate(services).Plugins;

            // Assert
            Assert.Same(clock, plugins.OfType<ServiceBoundPlugin>().Single().Clock);
        }

        [Fact]
        public void Should_create_the_plugin_the_registration_names_a_factory_for()
        {
            // Arrange
            using var services = CreateServices();
            var host = new Host();
            var registry = new PluginRegistry<IPlugin>().Add(_ => host);

            // Act
            var plugins = registry.Activate(services).Plugins;

            // Assert
            Assert.Same(host, Assert.Single(plugins));
        }
    }

    public sealed class Options
    {
        [Fact]
        public void Should_create_fresh_options_for_every_owner()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>()
                .Add<CountingPlugin, CountOptions>(options => options.Count = 7);

            // Act
            var first = registry.Activate(services).Plugins.OfType<CountingPlugin>().Single().Options;
            var second = registry.Activate(services).Plugins.OfType<CountingPlugin>().Single().Options;

            // Assert
            Assert.Multiple(
                () => Assert.Equal(7, first.Count),
                () => Assert.Equal(7, second.Count),
                () => Assert.NotSame(first, second));
        }

        [Fact]
        public void Should_leave_a_plugin_its_own_options_when_the_registration_configures_none()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>().Add<CountingPlugin>();

            // Act
            var plugin = registry.Activate(services).Plugins.OfType<CountingPlugin>().Single();

            // Assert
            Assert.Equal(1, plugin.Options.Count);
        }
    }

    public sealed class Defaults
    {
        [Fact]
        public void Should_add_a_default_the_registry_registers_nothing_for()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>().TryAdd<CountingPlugin>();

            // Act
            var plugins = registry.Activate(services).Plugins;

            // Assert
            Assert.IsType<CountingPlugin>(Assert.Single(plugins));
        }

        [Fact]
        public void Should_keep_what_the_registry_registered_over_a_default_asked_for_first()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>()
                .TryAdd<CountingPlugin>()
                .Add<CountingPlugin, CountOptions>(options => options.Count = 4);

            // Act
            var plugin = Assert.IsType<CountingPlugin>(Assert.Single(registry.Activate(services).Plugins));

            // Assert
            Assert.Equal(4, plugin.Options.Count);
        }

        [Fact]
        public void Should_keep_what_the_registry_registered_over_a_default_asked_for_last()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>()
                .Add<CountingPlugin, CountOptions>(options => options.Count = 4)
                .TryAdd<CountingPlugin>();

            // Act
            var plugin = Assert.IsType<CountingPlugin>(Assert.Single(registry.Activate(services).Plugins));

            // Assert
            Assert.Equal(4, plugin.Options.Count);
        }

        [Fact]
        public void Should_keep_only_the_last_of_two_explicit_registrations()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>()
                .Add<CountingPlugin, CountOptions>(options => options.Count = 2)
                .Add<CountingPlugin, CountOptions>(options => options.Count = 9);

            // Act
            var plugin = Assert.IsType<CountingPlugin>(Assert.Single(registry.Activate(services).Plugins));

            // Assert
            Assert.Equal(9, plugin.Options.Count);
        }
    }

    public sealed class Refusals
    {
        [Fact]
        public void Should_name_the_plugin_and_the_parameter_nothing_can_supply()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>().Add<NeedyPlugin>();

            // Act
            var refusal = Assert.Throws<InvalidOperationException>(() => registry.Activate(services));

            // Assert
            Assert.Multiple(
                () => Assert.Contains(nameof(NeedyPlugin), refusal.Message, StringComparison.Ordinal),
                () => Assert.Contains(nameof(Uri), refusal.Message, StringComparison.Ordinal),
                () => Assert.Contains("address", refusal.Message, StringComparison.Ordinal));
        }

        [Fact]
        public void Should_name_a_plugin_offering_more_than_one_constructor()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>().Add<AmbiguousPlugin>();

            // Act
            var refusal = Assert.Throws<InvalidOperationException>(() => registry.Activate(services));

            // Assert
            Assert.Contains(nameof(AmbiguousPlugin), refusal.Message, StringComparison.Ordinal);
        }
    }

    public sealed class Ownership
    {
        [Fact]
        public async Task Should_dispose_a_plugin_it_built_itself()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>().Add<ClosablePlugin>();
            var scope = registry.Activate(services);
            var plugin = scope.Plugins.OfType<ClosablePlugin>().Single();

            // Act
            await scope.DisposeAsync();

            // Assert
            Assert.True(plugin.IsDisposed);
        }

        [Fact]
        public async Task Should_leave_a_plugin_a_factory_supplied_to_whoever_wrote_the_factory()
        {
            // Arrange
            using var services = CreateServices();
            using var plugin = new ClosablePlugin();
            var registry = new PluginRegistry<IPlugin>().Add(_ => plugin);
            var scope = registry.Activate(services);

            // Act
            await scope.DisposeAsync();

            // Assert
            Assert.False(plugin.IsDisposed);
        }

        [Fact]
        public void Should_name_the_registration_a_plugin_sits_under_as_its_owner()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>()
                .Add<Host>(host => host.Add<HostBoundPlugin>());

            // Act
            var instances = registry.Activate(services).Instances;

            // Assert
            var host = instances.Single(instance => instance.Plugin is Host);
            Assert.Multiple(
                () => Assert.Null(host.Owner),
                () => Assert.Same(host, instances.Single(instance => instance.Plugin is HostBoundPlugin).Owner));
        }
    }

    public sealed class Description
    {
        [Fact]
        public void Should_drop_what_a_replaced_registration_had_registered_under_it()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>()
                .Add<Host>(host => host.Add<HostBoundPlugin>())
                .Add<Host>(host => host.Add<AlsoHostBoundPlugin>());

            // Act
            var names = registry.Activate(services).Plugins.Select(plugin => plugin.Id).ToList();

            // Assert
            Assert.Equal(["host", "also-host-bound"], names);
        }

        [Fact]
        public void Should_order_a_registration_ahead_of_what_is_registered_under_it()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>()
                .Add<Host>(host => host.Add<HostBoundPlugin>())
                .Add<CountingPlugin>();

            // Act
            var names = registry.Activate(services).Plugins.Select(plugin => plugin.Id).ToList();

            // Assert
            Assert.Equal(["host", "host-bound", "counting"], names);
        }

        [Fact]
        public void Should_leave_the_rest_in_place_when_one_registration_is_replaced()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>()
                .Add<CountingPlugin>()
                .Add<ClosablePlugin>()
                .Add<CountingPlugin, CountOptions>(options => options.Count = 5);

            // Act
            var names = registry.Activate(services).Plugins.Select(plugin => plugin.Id).ToList();

            // Assert
            Assert.Equal(["counting", "closable"], names);
        }

        [Fact]
        public void Should_build_one_plugin_per_place_it_is_registered()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>()
                .Add<Host>(host => host.Add<CountingPlugin, CountOptions>(options => options.Count = 3))
                .Add<SecondHost>(host => host.Add<CountingPlugin, CountOptions>(options => options.Count = 8));

            // Act
            var counting = registry.Activate(services).Plugins.OfType<CountingPlugin>().ToList();

            // Assert
            Assert.Multiple(
                () => Assert.Equal(2, counting.Count),
                () => Assert.NotSame(counting[0], counting[1]),
                () => Assert.Equal([3, 8], counting.Select(plugin => plugin.Options.Count)));
        }

        [Fact]
        public void Should_keep_a_plugin_registered_under_another_one_apart()
        {
            // Arrange
            using var services = CreateServices();
            var registry = new PluginRegistry<IPlugin>()
                .Add<Host>(host => host.Add<HostBoundPlugin>())
                .Add<SecondHost>();

            // Act
            var plugins = registry.Activate(services).Plugins;

            // Assert
            Assert.Collection(
                plugins,
                plugin => Assert.IsType<Host>(plugin),
                plugin => Assert.IsType<HostBoundPlugin>(plugin),
                plugin => Assert.IsType<SecondHost>(plugin));
        }

        [Fact]
        public void Should_activate_nothing_for_a_registry_holding_nothing()
        {
            // Arrange
            using var services = CreateServices();

            // Act
            var plugins = new PluginRegistry<IPlugin>().Activate(services).Plugins;

            // Assert
            Assert.Empty(plugins);
        }
    }
}
