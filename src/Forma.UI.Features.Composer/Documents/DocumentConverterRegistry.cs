namespace Forma.UI.Features.Composer.Documents;

using System.Collections.Concurrent;

/// <summary>The converters available to the composer, keyed by the formats they read and write.</summary>
public sealed class DocumentConverterRegistry
{
    private readonly ConcurrentDictionary<(ComposerDocumentFormat Source, ComposerDocumentFormat Target), IComposerDocumentConverter> _converters = new();

    /// <summary>Creates an empty registry.</summary>
    public DocumentConverterRegistry()
    {
    }

    /// <summary>Creates a registry holding the given converters.</summary>
    public DocumentConverterRegistry(IEnumerable<IComposerDocumentConverter> converters)
    {
        ArgumentNullException.ThrowIfNull(converters);

        foreach (var converter in converters)
        {
            Register(converter);
        }
    }

    /// <summary>Adds a converter, replacing any already registered for the same pair of formats.</summary>
    public void Register(IComposerDocumentConverter converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        _converters[(converter.Source, converter.Target)] = converter;
    }

    /// <summary>The converter between the two formats, or null when none is registered.</summary>
    public IComposerDocumentConverter? Find(ComposerDocumentFormat source, ComposerDocumentFormat target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        return _converters.GetValueOrDefault((source, target));
    }

    /// <summary>Whether the composer can carry a document from one format to the other.</summary>
    public bool CanConvert(ComposerDocumentFormat source, ComposerDocumentFormat target)
        => source == target || Find(source, target) is not null;

    /// <summary>Converts the document, refusing when no converter is registered.</summary>
    public ComposerConversionResult Convert(ComposerDocument document, ComposerDocumentFormat targetFormat)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(targetFormat);

        if (document.Format == targetFormat)
        {
            return ComposerConversionResult.Success(document.Content);
        }

        var converter = Find(document.Format, targetFormat);

        return converter is null
            ? ComposerConversionResult.Refused([$"No converter is registered from '{document.Format}' to '{targetFormat}'."])
            : converter.Convert(document.Content);
    }
}
