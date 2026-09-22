namespace Forma.Framework.Localization;

using System.Reflection;

using Microsoft.Extensions.Logging;

/// <summary>The locale packs an assembly was built against, named by the <see cref="LocalePackAttribute"/>s its build wrote into it.</summary>
public static class LocalePackLoader
{
    /// <summary>The name of every pack the assembly was built against, in the order the attributes were written, each named once.</summary>
    /// <param name="assembly">The assembly whose build stamped the names, the entry assembly in a host that references its packs directly.</param>
    public static IReadOnlyList<string> GetNames(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return [.. assembly
            .GetCustomAttributes<LocalePackAttribute>()
            .Select(pack => pack.AssemblyName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)];
    }

    /// <summary>The packs the assembly was built against, loaded by name; one that cannot be loaded is reported and left out, so a host whose deployment lost a language still renders in the language its keys are written in.</summary>
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

    // A pack is named by a build that has finished, so what is left is a deployment that lost the file or a trimmer that took it.
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
