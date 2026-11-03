namespace Forma.Framework.Localization;

using System.Reflection;

using Forma.Framework.Localization.Files;

using Microsoft.Extensions.Logging;

/// <summary>Settings shared by every localization backend.</summary>
public sealed record LocalizationOptions
{
    /// <summary>The language the localization keys are written in.</summary>
    public string SourceLanguage { get; init; } = "en";

    /// <summary>Whether a key with no translation for the current UI culture throws.</summary>
    public bool RequireMatchingLocalization { get; init; }

    /// <summary>The folder holding the host's own locale files, a relative one counted from the application's base directory; empty for none.</summary>
    public string LocalesPath { get; init; } = LocaleConfig.DefaultLocalesDir;

    /// <summary>The locale packs read for every assembly beside its own files, in the order given.</summary>
    public IReadOnlyList<Assembly> LocalePacks { get; init; } = [];

    /// <summary>The assembly a host's own locale files are embedded in, read where the host's folder is read, before it.</summary>
    public Assembly? HostLocaleAssembly { get; init; }

    /// <summary>The options with what an application's build recorded in its assembly: the packs it references, placed before the ones named, each once, and the host's own locale files where it embedded them.</summary>
    /// <param name="application">The assembly whose build recorded the packs and embedded the host's files.</param>
    /// <param name="logger">Where a pack that cannot be loaded is reported.</param>
    public LocalizationOptions WithRecordedLocales(Assembly application, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(logger);

        var recorded = LocalePackLoader.Load(application, logger);
        var configured = recorded.Count == 0
            ? this
            : this with { LocalePacks = [.. recorded, .. LocalePacks.Where(pack => !recorded.Contains(pack))] };

        return application.IsDefined(typeof(HostLocalesAttribute), inherit: false)
            ? configured with { HostLocaleAssembly = application }
            : configured;
    }
}
