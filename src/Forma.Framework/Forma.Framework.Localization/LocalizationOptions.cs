namespace Forma.Framework.Localization;

using System.Reflection;

using Forma.Framework.Localization.Files;

using Microsoft.Extensions.Logging;

/// <summary>Settings shared by every localization backend.</summary>
public sealed record LocalizationOptions
{
    /// <summary>Language used by the localization keys and their language-specific rules when no translation is available.</summary>
    public string SourceLanguage { get; init; } = "en";

    /// <summary>Whether a key with no translation for the culture on screen throws rather than falling back to itself.</summary>
    public bool RequireMatchingLocalization { get; init; }

    /// <summary>Folder holding the host's own locale files, resolved against the application's base directory when relative; empty to read none.</summary>
    public string LocalesPath { get; init; } = LocaleConfig.DefaultLocalesDir;

    /// <summary>The locale packs read for every assembly beside its own files, in the order given; none unless named. A registrar adds the packs the application was built against through <see cref="WithRecordedPacks"/>, so a host names here only the packs its build did not record, as a test whose entry assembly is its runner's does.</summary>
    public IReadOnlyList<Assembly> LocalePacks { get; init; } = [];

    /// <summary>The options with the packs an application's build recorded in its assembly laid before the ones named, each once; a pack that cannot be loaded is reported and left out.</summary>
    /// <param name="application">The assembly whose build recorded the packs, the entry assembly in an application that references its packs directly.</param>
    /// <param name="logger">Where a pack that cannot be loaded is reported.</param>
    public LocalizationOptions WithRecordedPacks(Assembly application, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(logger);

        var recorded = LocalePackLoader.Load(application, logger);

        return recorded.Count == 0
            ? this
            : this with { LocalePacks = [.. recorded, .. LocalePacks.Where(pack => !recorded.Contains(pack))] };
    }
}
