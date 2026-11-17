namespace Forma.Framework.Localization;

using System.Reflection;

using Forma.Framework.Localization.Files;

using Microsoft.Extensions.Logging;

/// <summary>Settings shared by every localization backend.</summary>
public sealed record LocalizationOptions
{
    /// <summary>The language the localization keys are written in.</summary>
    public string SourceLanguage { get; init; } = "en";

    /// <summary>Whether a key without a localization for the current UI culture throws, outside the source language, whose text is the key itself.</summary>
    public bool RequireMatchingLocalization { get; init; }

    /// <summary>The checks run on every lookup, which report what they find through the application's logging, or null to leave lookups unchecked.</summary>
    public LocalizationDiagnostics? Diagnostics { get; init; }

    /// <summary>The folder holding the host's own locale files, a relative one counted from the application's base directory; empty for none.</summary>
    public string LocalesPath { get; init; } = LocaleConfig.DefaultLocalesDir;

    /// <summary>The locale packs read for every assembly beside its own files, in the order given.</summary>
    public IReadOnlyList<Assembly> LocalePacks { get; init; } = [];

    /// <summary>The applications whose embedded locale files are read for other assemblies' types, before the folder beside the running application, in the order given.</summary>
    public IReadOnlyList<Assembly> HostLocaleAssemblies { get; init; } = [];

    /// <summary>The options with what an application's build recorded in its assembly: the packs it references, placed before the ones named, each once, and the applications whose files it reads, each one it names and then itself, placed before the ones named, each once.</summary>
    /// <param name="application">The application, whose build recorded the packs and the applications it names.</param>
    /// <param name="logger">Where a pack or a named application that cannot be loaded is reported.</param>
    public LocalizationOptions WithRecordedLocales(Assembly application, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(logger);

        var recordedPacks = LocaleAssemblyLoader.LoadPacks(application, logger);
        var configuredOptions = recordedPacks.Count == 0
            ? this
            : this with { LocalePacks = [.. recordedPacks, .. LocalePacks.Where(pack => !recordedPacks.Contains(pack))] };

        var hosts = LocaleAssemblyLoader.LoadHostLocaleAssemblies(application, logger);

        return hosts.Count == 0
            ? configuredOptions
            : configuredOptions with { HostLocaleAssemblies = [.. hosts, .. HostLocaleAssemblies.Where(host => !hosts.Contains(host))] };
    }
}
