namespace Forma.Framework.Plugins;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

/// <summary>Builds one owner's plugins from its registrations, resolving what each constructor asks for.</summary>
internal static class PluginActivator
{
    public static PluginScope<TPlugin> Activate<TPlugin>(
        IReadOnlyList<PluginRegistration<TPlugin>> registrations,
        IServiceProvider services)
        where TPlugin : class, IPlugin
    {
        // The same plugin registered in two places is two plugins, each with its own options.
        var built = new Dictionary<PluginRegistration<TPlugin>, PluginInstance<TPlugin>>();
        var underway = new HashSet<PluginRegistration<TPlugin>>();

        foreach (var registration in registrations)
        {
            Build(registration, registrations, services, built, underway);
        }

        return new PluginScope<TPlugin>([.. registrations.Select(registration => built[registration])]);
    }

    private static PluginInstance<TPlugin> Build<TPlugin>(
        PluginRegistration<TPlugin> registration,
        IReadOnlyList<PluginRegistration<TPlugin>> registrations,
        IServiceProvider services,
        Dictionary<PluginRegistration<TPlugin>, PluginInstance<TPlugin>> built,
        HashSet<PluginRegistration<TPlugin>> underway)
        where TPlugin : class, IPlugin
    {
        if (built.TryGetValue(registration, out var already))
        {
            return already;
        }

        if (!underway.Add(registration))
        {
            throw new InvalidOperationException(
                $"{registration.PluginType.Name} takes a plugin that takes it back, so neither can be built; break the cycle by having one of them ask for what it needs at the point it is used instead.");
        }

        TPlugin plugin;

        if (registration.Factory is { } factory)
        {
            underway.Remove(registration);
            plugin = factory(services);
        }
        else
        {
            var constructor = GetConstructor(registration.PluginType);
            var arguments = constructor
                .GetParameters()
                .Select(parameter => Resolve(parameter, registration, registrations, services, built, underway))
                .ToArray();

            underway.Remove(registration);
            plugin = (TPlugin)constructor.Invoke(arguments);
        }

        // The owner is built before whatever sits under it, so its instance is already there to point at.
        var instance = new PluginInstance<TPlugin>
        {
            Plugin = plugin,
            Owner = registration.Owner is { } owner && built.TryGetValue(owner, out var ownerInstance) ? ownerInstance : null,
            IsAutonomous = registration.Factory is not null,
        };

        built[registration] = instance;

        return instance;
    }

    private static object? Resolve<TPlugin>(
        ParameterInfo parameter,
        PluginRegistration<TPlugin> registration,
        IReadOnlyList<PluginRegistration<TPlugin>> registrations,
        IServiceProvider services,
        Dictionary<PluginRegistration<TPlugin>, PluginInstance<TPlugin>> built,
        HashSet<PluginRegistration<TPlugin>> underway)
        where TPlugin : class, IPlugin
    {
        // A plugin registered beside this one is one instance, shared by everything that takes it.
        if (GetSupplier(parameter.ParameterType, registration, registrations) is { } supplier)
        {
            return Build(supplier, registrations, services, built, underway).Plugin;
        }

        if (registration.OptionsType == parameter.ParameterType)
        {
            var options = Activator.CreateInstance(registration.OptionsType)!;

            registration.Configure?.Invoke(options);

            return options;
        }

        if (services.GetService(parameter.ParameterType) is { } service)
        {
            return service;
        }

        if (parameter.HasDefaultValue)
        {
            return parameter.DefaultValue;
        }

        throw new InvalidOperationException(
            $"{registration.PluginType.Name} takes a {parameter.ParameterType.Name} called '{parameter.Name}' that nothing can supply: no plugin registered beside it is one, the registration configured no options of that type, the host registered no such service, and the parameter has no default.");
    }

    private static PluginRegistration<TPlugin>? GetSupplier<TPlugin>(
        Type parameterType,
        PluginRegistration<TPlugin> registration,
        IReadOnlyList<PluginRegistration<TPlugin>> registrations)
        where TPlugin : class, IPlugin
    {
        // The nearest owner implementing the contract answers before the first registration that does.
        for (var owner = registration.Owner; owner is not null; owner = owner.Owner)
        {
            if (parameterType.IsAssignableFrom(owner.PluginType))
            {
                return owner;
            }
        }

        return registrations.FirstOrDefault(candidate => parameterType.IsAssignableFrom(candidate.PluginType));
    }

    private static ConstructorInfo GetConstructor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type pluginType)
    {
        var constructors = pluginType.GetConstructors();

        return constructors.Length == 1
            ? constructors[0]
            : throw new InvalidOperationException(
                $"{pluginType.Name} offers {constructors.Length} public constructors where activation needs exactly one, so there is no way to tell which describes the plugin.");
    }
}
