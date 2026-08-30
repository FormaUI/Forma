namespace Forma.UI.Features.Composer.Plugins;

using System.Reflection;

/// <summary>Finds the feature and view plugins an assembly ships and builds them against the plugins already chosen.</summary>
public static class ComposerPluginDiscovery
{
    /// <summary>Builds every feature and view plugin found, appended after the plugins given.</summary>
    /// <param name="plugins">The plugins already constructed — the editors, and any feature plugin needing configuration of its own. A found plugin's constructor parameters are satisfied from these, and a type among them is never built a second time.</param>
    /// <param name="assembly">The assembly to search, or null to search every loaded assembly that takes the composer.</param>
    /// <param name="locations">Namespace suffixes narrowing the search, or empty for the whole assembly.</param>
    /// <returns>The plugins given, in their order, followed by the plugins found.</returns>
    /// <exception cref="InvalidOperationException">A found plugin takes something no given plugin can stand in for. Construct that plugin and pass it among <paramref name="plugins"/>.</exception>
    public static IReadOnlyList<IComposerPlugin> Discover(
        IReadOnlyList<IComposerPlugin> plugins,
        Assembly? assembly = null,
        IReadOnlyList<string>? locations = null)
    {
        ArgumentNullException.ThrowIfNull(plugins);

        var held = plugins.Select(plugin => plugin.GetType()).ToHashSet();
        var found = Candidates(assembly)
            .Where(candidate => !held.Contains(candidate) && LivesIn(candidate, locations))
            .OrderBy(candidate => candidate.FullName, StringComparer.Ordinal)
            .Select(candidate => Build(candidate, plugins));

        return [.. plugins, .. found];
    }

    private static IEnumerable<Type> Candidates(Assembly? assembly)
    {
        IEnumerable<Assembly> assemblies = assembly is null ? TakingComposer() : [assembly];

        return assemblies
            .SelectMany(TypesOf)
            .Where(candidate => candidate is { IsClass: true, IsAbstract: false, IsVisible: true }
                && (typeof(IComposerFeaturePlugin).IsAssignableFrom(candidate) || typeof(IComposerViewPlugin).IsAssignableFrom(candidate)));
    }

    private static IEnumerable<Assembly> TakingComposer()
    {
        var composer = typeof(IComposerPlugin).Assembly;
        var name = composer.GetName().Name;

        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(candidate => !candidate.IsDynamic
                && (candidate == composer
                    || candidate.GetReferencedAssemblies().Any(reference => string.Equals(reference.Name, name, StringComparison.OrdinalIgnoreCase))));
    }

    private static IEnumerable<Type> TypesOf(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException incomplete)
        {
            return incomplete.Types.Where(loaded => loaded is not null)!;
        }
    }

    private static bool LivesIn(Type candidate, IReadOnlyList<string>? locations)
    {
        if (locations is null or [])
        {
            return true;
        }

        var space = candidate.Namespace ?? string.Empty;

        return locations
            .Select(location => location.Replace('/', '.').Replace('\\', '.').Trim('.'))
            .Any(location => string.Equals(space, location, StringComparison.Ordinal)
                || space.EndsWith($".{location}", StringComparison.Ordinal));
    }

    private static IComposerPlugin Build(Type found, IReadOnlyList<IComposerPlugin> plugins)
    {
        var constructors = found.GetConstructors();

        if (constructors.Length != 1)
        {
            throw new InvalidOperationException(
                $"{found.Name} offers {constructors.Length} public constructors where discovery needs exactly one; construct it yourself and pass it among the plugins given.");
        }

        var parameters = constructors[0].GetParameters();
        var arguments = new object?[parameters.Length];

        for (var position = 0; position < parameters.Length; position++)
        {
            var parameter = parameters[position];
            var given = plugins.FirstOrDefault(parameter.ParameterType.IsInstanceOfType);

            arguments[position] = given is not null
                ? given
                : parameter.HasDefaultValue
                    ? parameter.DefaultValue
                    : throw new InvalidOperationException(
                        $"{found.Name} takes a {parameter.ParameterType.Name} no given plugin can stand in for; construct it yourself and pass it among the plugins given.");
        }

        return (IComposerPlugin)constructors[0].Invoke(arguments);
    }
}
