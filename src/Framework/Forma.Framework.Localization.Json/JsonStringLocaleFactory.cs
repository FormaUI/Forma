namespace Forma.Framework.Localization.Json;

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

using Forma.Framework.Localization;
using Forma.Framework.Localization.Files;
using Forma.Framework.Localization.Language;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>Creates locales that read their strings from JSON files embedded in the assembly owning the type, with the locale packs the options name and then the host's own files laid over them.</summary>
public sealed class JsonStringLocaleFactory : IStringLocaleFactory
{
    private readonly ConcurrentDictionary<string, IStringLocale> _locales = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Type, IStringLocale> _localesByType = new();
    private readonly JsonLocaleContext _context;

    /// <summary>Creates a factory with the default options.</summary>
    public JsonStringLocaleFactory()
        : this(new LocalizationOptions())
    {
    }

    /// <summary>Creates a factory configured by the given options.</summary>
    public JsonStringLocaleFactory(LocalizationOptions options)
        : this(options, NullLoggerFactory.Instance)
    {
    }

    /// <summary>Creates a factory configured by the given options, logging through the given factory.</summary>
    public JsonStringLocaleFactory(LocalizationOptions options, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        // A regional value stays regional, since CLDR keys a few of its rule sets by region.
        var sourceCulture = GetCulture(options.SourceLanguage);
        CultureInfo[]? diagnosedCultures = options.Diagnostics is { } diagnostics ? [.. diagnostics.Cultures.Select(GetCulture)] : null;

        var format = new JsonLocaleFileFormat();
        var logger = loggerFactory.CreateLogger<JsonStringLocaleFactory>();
        var cache = new JsonResourceCache(format, GetLocalesPath(options.LocalesPath), options.LocalePacks, options.HostLocaleAssemblies, logger);

        _context = new JsonLocaleContext(
            cache,
            sourceCulture,
            new PluralExpressionEvaluator(CommonPluralRuleSet.Default, loggerFactory.CreateLogger<PluralExpressionEvaluator>(), options.Diagnostics),
            options,
            diagnosedCultures is null ? null : new JsonLocaleDiagnostics(cache, diagnosedCultures, logger));
    }

    /// <inheritdoc />
    public IStringLocale Create(Type resourceSource)
    {
        ArgumentNullException.ThrowIfNull(resourceSource);

        return _localesByType.GetOrAdd(resourceSource, static (type, factory) => factory.CreateForType(type), this);
    }

    /// <inheritdoc />
    public IStringLocale Create(string baseName, string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        var assembly = Assembly.Load(new AssemblyName(location));
        var relativeName = TrimPrefix(baseName, location + ".");

        return GetOrAdd(assembly, assembly.GetName().Name ?? string.Empty, Qualify(location, relativeName), $"{location}.{relativeName}");
    }

    private IStringLocale CreateForType(Type resourceSource)
    {
        // The open generic definition names the type its files are read for.
        var source = resourceSource.IsConstructedGenericType ? resourceSource.GetGenericTypeDefinition() : resourceSource;
        var assembly = source.Assembly;
        var assemblyName = assembly.GetName().Name ?? string.Empty;
        var typeName = NormalizeTypeName(source.FullName ?? source.Name);

        return GetOrAdd(assembly, assemblyName, GetBaseName(assemblyName, typeName), typeName);
    }

    // The culture an option names, where a name naming the invariant culture throws as an unknown one does.
    private static CultureInfo GetCulture(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var culture = CultureInfo.GetCultureInfo(name);
        if (culture.Name.Length == 0)
        {
            throw new ArgumentException($"'{name}' doesn't name a language.", nameof(name));
        }

        return culture;
    }

    private static string GetLocalesPath(string path)
        => path.Length == 0 ? string.Empty : Path.GetFullPath(path, AppContext.BaseDirectory);

    private static string TrimPrefix(string value, string prefix)
        => value.StartsWith(prefix, StringComparison.Ordinal) ? value[prefix.Length..] : value;

    // Nested types arrive as Outer+Inner and generics as Name`1.
    private static string NormalizeTypeName(string typeName)
    {
        var segments = typeName.Replace('+', '.').Split('.');
        for (var i = 0; i < segments.Length; i++)
        {
            var arity = segments[i].IndexOf('`', StringComparison.Ordinal);
            if (arity >= 0)
            {
                segments[i] = segments[i][..arity];
            }
        }

        return string.Join('.', segments);
    }

    // A type outside any namespace is qualified by the assembly's name; any other full name is the base name as it stands.
    private static string GetBaseName(string assemblyName, string typeName)
        => typeName.StartsWith(assemblyName + ".", StringComparison.Ordinal)
            ? Qualify(assemblyName, typeName[(assemblyName.Length + 1)..])
            : typeName.Contains('.', StringComparison.Ordinal) ? typeName : Qualify(assemblyName, typeName);

    private static string Qualify(string assemblyName, string relativeName)
        => $"{assemblyName}.{relativeName}";

    private IStringLocale GetOrAdd(Assembly assembly, string assemblyName, string baseName, string typeName)
        => _locales.GetOrAdd($"{assembly.FullName}|{baseName}", _ => new JsonStringLocale(_context, new ResourceLocation(assembly, assemblyName, baseName, typeName)));
}
