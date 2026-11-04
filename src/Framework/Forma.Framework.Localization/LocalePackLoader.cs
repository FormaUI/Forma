namespace Forma.Framework.Localization;

using System.Reflection;

using Forma.Framework.Localization.Files;

using Microsoft.Extensions.Logging;

/// <summary>Reads what an application's build recorded of its locales: the locale packs it was built against, from the <see cref="LocalePackAttribute"/>s its build wrote into it, and the assemblies carrying a host's files, its own where its build embedded them and each one a <see cref="LocaleReferenceAttribute"/> names.</summary>
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

    /// <summary>The assemblies an application's build recorded its locales in: the entry assembly, or, where the runtime names none, as a native host such as .NET for Android's and for iOS's names none, each loaded assembly whose build embedded a host's files, named an assembly that did, or recorded a pack.</summary>
    /// <param name="entry">The assembly the runtime started the application from.</param>
    /// <param name="loaded">The assemblies loaded into the application.</param>
    public static IReadOnlyList<Assembly> GetApplicationAssemblies(Assembly? entry, IEnumerable<Assembly> loaded)
    {
        ArgumentNullException.ThrowIfNull(loaded);

        return entry is not null
            ? [entry]
            : [.. loaded.Where(assembly => !assembly.IsDynamic && (EmbedsHostFiles(assembly)
                || assembly.IsDefined(typeof(LocaleReferenceAttribute), inherit: false)
                || assembly.IsDefined(typeof(LocalePackAttribute), inherit: false)))];
    }

    /// <summary>Whether the assembly's build embedded a host's files in it, under <see cref="LocalePackSource.HostResourcePrefix"/>.</summary>
    /// <param name="assembly">The assembly.</param>
    public static bool EmbedsHostFiles(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return !assembly.IsDynamic && assembly.GetManifestResourceNames().Any(name => name.StartsWith(LocalePackSource.HostResourcePrefix, StringComparison.Ordinal));
    }

    /// <summary>The assemblies carrying a host's files the application reads: each one its attributes name, loaded by name, in their order, then the application itself where its build embedded them.</summary>
    /// <param name="application">The assembly whose build recorded them.</param>
    /// <param name="logger">Where a named assembly that cannot be loaded is reported.</param>
    public static IReadOnlyList<Assembly> LoadHostLocaleAssemblies(Assembly application, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(logger);

        var assemblies = new List<Assembly>();
        foreach (var name in application.GetCustomAttributes<LocaleReferenceAttribute>().Select(host => host.AssemblyName).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.Ordinal))
        {
            if (LoadHostLocaleAssembly(name, logger) is { } assembly && !assemblies.Contains(assembly))
            {
                assemblies.Add(assembly);
            }
        }

        if (EmbedsHostFiles(application) && !assemblies.Contains(application))
        {
            assemblies.Add(application);
        }

        return assemblies;
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

    private static Assembly? LoadHostLocaleAssembly(string name, ILogger logger)
    {
        try
        {
            return Assembly.Load(new AssemblyName(name));
        }
        catch (Exception exception) when (exception is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            LocalizationLog.HostLocaleAssemblyNotLoaded(logger, name, exception.Message);

            return null;
        }
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
