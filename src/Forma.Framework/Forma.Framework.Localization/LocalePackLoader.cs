namespace Forma.Framework.Localization;

using System.Reflection;

using Microsoft.Extensions.Logging;

/// <summary>Reads the locale packs an assembly was built against from the <see cref="LocalePackAttribute"/>s its build wrote into it.</summary>
public static class LocalePackLoader
{
    /// <summary>The name of every pack the assembly was built against, in the order the attributes were written, each named once.</summary>
    /// <param name="assembly">The assembly whose build stamped the names.</param>
    public static IReadOnlyList<string> GetNames(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return [.. assembly
            .GetCustomAttributes<LocalePackAttribute>()
            .Select(pack => pack.AssemblyName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)];
    }

    /// <summary>The assemblies an application's build recorded its locales in: the entry assembly, or, where the runtime names none, as .NET for Android and for iOS don't, each loaded assembly whose build recorded a pack or embedded the host's own files.</summary>
    /// <param name="entry">The assembly the runtime started the application from.</param>
    /// <param name="loaded">The assemblies loaded into the application.</param>
    public static IReadOnlyList<Assembly> GetApplicationAssemblies(Assembly? entry, IEnumerable<Assembly> loaded)
    {
        ArgumentNullException.ThrowIfNull(loaded);

        return entry is not null
            ? [entry]
            : [.. loaded.Where(assembly => assembly.IsDefined(typeof(HostLocalesAttribute), inherit: false) || assembly.IsDefined(typeof(LocalePackAttribute), inherit: false))];
    }

    /// <summary>The packs the assembly was built against, loaded by name.</summary>
    /// <param name="assembly">The assembly whose build stamped the names.</param>
    /// <param name="logger">Where a pack that cannot be loaded is reported.</param>
    public static IReadOnlyList<Assembly> Load(Assembly assembly, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(logger);

        var packs = new List<Assembly>();
        foreach (var name in GetNames(assembly))
        {
            if (LoadPack(name, logger) is { } pack)
            {
                packs.Add(pack);
            }
        }

        return packs;
    }

    private static Assembly? LoadPack(string name, ILogger logger)
    {
        try
        {
            return Assembly.Load(new AssemblyName(name));
        }
        catch (Exception exception) when (exception is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            LocalizationLog.LocalePackNotLoaded(logger, name, exception.Message);

            return null;
        }
    }
}
