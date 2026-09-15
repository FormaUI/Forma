namespace Forma.Framework.Composer.Plugins;

using System.Reflection;

/// <summary>Builds one composer's plugins from its description, resolving what each constructor asks for.</summary>
internal static class ComposerPluginActivator
{
    public static ComposerPluginSet Activate(
        IReadOnlyList<ComposerPluginRegistration> registrations,
        IServiceProvider services)
    {
        // Keyed by registration rather than by type, because the same plugin registered in two places is
        // two plugins: one per place, each with its own options.
        var built = new Dictionary<ComposerPluginRegistration, IComposerPlugin>();
        var underway = new HashSet<ComposerPluginRegistration>();

        foreach (var registration in registrations)
        {
            Build(registration, registrations, services, built, underway);
        }

        // Registration order is the order the composer lays plugins out in, so the built set keeps it.
        var plugins = registrations.Select(registration => built[registration]).ToList();
        var owned = registrations
            .Where(registration => registration.Factory is null)
            .Select(registration => built[registration])
            .ToList();

        return new ComposerPluginSet(plugins, owned);
    }

    private static IComposerPlugin Build(
        ComposerPluginRegistration registration,
        IReadOnlyList<ComposerPluginRegistration> registrations,
        IServiceProvider services,
        Dictionary<ComposerPluginRegistration, IComposerPlugin> built,
        HashSet<ComposerPluginRegistration> underway)
    {
        if (built.TryGetValue(registration, out var already))
        {
            return already;
        }

        if (!underway.Add(registration))
        {
            throw new InvalidOperationException(
                $"{registration.PluginType.Name} takes a plugin that takes it back, so neither can be built; break the cycle by having one of them ask the composer for what it needs instead.");
        }

        IComposerPlugin plugin;

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
            plugin = (IComposerPlugin)constructor.Invoke(arguments);
        }

        built[registration] = plugin;

        return plugin;
    }

    private static object? Resolve(
        ParameterInfo parameter,
        ComposerPluginRegistration registration,
        IReadOnlyList<ComposerPluginRegistration> registrations,
        IServiceProvider services,
        Dictionary<ComposerPluginRegistration, IComposerPlugin> built,
        HashSet<ComposerPluginRegistration> underway)
    {
        // A plugin this composer also has, so every plugin naming the same editor is handed the one instance of it.
        if (registrations.FirstOrDefault(candidate => parameter.ParameterType.IsAssignableFrom(candidate.PluginType)) is { } supplier)
        {
            return Build(supplier, registrations, services, built, underway);
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
            $"{registration.PluginType.Name} takes a {parameter.ParameterType.Name} called '{parameter.Name}' that nothing can supply: no plugin registered with this composer is one, the registration configured no options of that type, the host registered no such service, and the parameter has no default.");
    }

    private static ConstructorInfo GetConstructor(Type pluginType)
    {
        var constructors = pluginType.GetConstructors();

        return constructors.Length == 1
            ? constructors[0]
            : throw new InvalidOperationException(
                $"{pluginType.Name} offers {constructors.Length} public constructors where activation needs exactly one, so there is no way to tell which describes the plugin.");
    }
}
