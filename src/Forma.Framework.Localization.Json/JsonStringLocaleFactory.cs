namespace Forma.Framework.Localization.Json;

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

using Forma.Framework.Localization;
using Forma.Framework.Localization.Language;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>Creates locales that read their strings from JSON files embedded in the assembly owning the type, with the host's own files laid over them.</summary>
public sealed class JsonStringLocaleFactory : IStringLocaleFactory
{
    private readonly ConcurrentDictionary<string, IStringLocale> _locales = new(StringComparer.Ordinal);
    private readonly JsonLocaleContext _context;
    private readonly string _resourcesPath;

    /// <summary>Creates a factory reading files that sit beside the types they translate.</summary>
    public JsonStringLocaleFactory()
        : this(new JsonLocalizationOptions())
    {
    }

    /// <summary>Creates a factory configured by the given options, discarding what its locales would log.</summary>
    public JsonStringLocaleFactory(JsonLocalizationOptions options)
        : this(options, NullLoggerFactory.Instance)
    {
    }

    /// <summary>Creates a factory configured by the given options, logging through the given factory.</summary>
    public JsonStringLocaleFactory(JsonLocalizationOptions options, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SourceLanguage);

        // A regional value survives, because CLDR keys a few of its rule sets by region rather than language.
        var sourceCulture = CultureInfo.GetCultureInfo(options.SourceLanguage);
        if (sourceCulture.Name.Length == 0)
        {
            throw new ArgumentException($"'{options.SourceLanguage}' names no language.", nameof(options));
        }

        _context = new JsonLocaleContext(
            new JsonResourceCache(new LocalesFolder(GetLocalesPath(options.LocalesPath))),
            sourceCulture,
            new PluralExpressionEvaluator(CommonPluralRuleSet.Default, loggerFactory.CreateLogger<PluralExpressionEvaluator>()),
            options);

        _resourcesPath = NormalizePath(options.ResourcesPath);
    }

    /// <summary>Creates a locale for the strings belonging to a type.</summary>
    public IStringLocale Create(Type resourceSource)
    {
        ArgumentNullException.ThrowIfNull(resourceSource);

        // A closed generic carries its arguments in FullName; the open definition names the file.
        var source = resourceSource.IsConstructedGenericType ? resourceSource.GetGenericTypeDefinition() : resourceSource;
        var assembly = source.Assembly;
        var assemblyName = assembly.GetName().Name ?? string.Empty;
        var typeName = NormalizeTypeName(source.FullName ?? source.Name);

        return GetOrAdd(assembly, GetBaseName(assemblyName, typeName), typeName);
    }

    /// <summary>Creates a locale for a base name inside the named assembly.</summary>
    public IStringLocale Create(string baseName, string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        var assembly = Assembly.Load(new AssemblyName(location));

        return GetOrAdd(assembly, Qualify(location, TrimPrefix(baseName, location + ".")), baseName);
    }

    // A relative folder sits beside the application, wherever it runs from.
    private static string GetLocalesPath(string path)
        => path.Length == 0 ? string.Empty : Path.GetFullPath(path, AppContext.BaseDirectory);

    private static string NormalizePath(string path)
        => path.Replace('\\', '.').Replace('/', '.').Trim('.');

    private static string TrimPrefix(string value, string prefix)
        => value.StartsWith(prefix, StringComparison.Ordinal) ? value[prefix.Length..] : value;

    // Nested types arrive as Outer+Inner and generics as Name`1; neither shape appears in a file name.
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

    // A type's full name already spells the root namespace its resource was embedded under, which leads
    // with the assembly name only when a project left the two in step; a type without one carries no root.
    private string GetBaseName(string assemblyName, string typeName)
        => typeName.StartsWith(assemblyName + ".", StringComparison.Ordinal)
            ? Qualify(assemblyName, typeName[(assemblyName.Length + 1)..])
            : typeName.Contains('.', StringComparison.Ordinal) ? typeName : Qualify(assemblyName, typeName);

    private string Qualify(string assemblyName, string relativeName)
        => _resourcesPath.Length == 0
            ? $"{assemblyName}.{relativeName}"
            : $"{assemblyName}.{_resourcesPath}.{relativeName}";

    private IStringLocale GetOrAdd(Assembly assembly, string baseName, string typeName)
        => _locales.GetOrAdd($"{assembly.FullName}|{baseName}", _ => new JsonStringLocale(_context, new ResourceLocation(assembly, baseName, typeName)));
}
