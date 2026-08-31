namespace Forma.Features.Composer.Documents;

/// <summary>Converts document content from one format to another.</summary>
public interface IComposerDocumentConverter
{
    /// <summary>The format the converter reads.</summary>
    ComposerDocumentFormat Source { get; }

    /// <summary>The format the converter writes.</summary>
    ComposerDocumentFormat Target { get; }

    /// <summary>Converts the content, reporting what it could not represent.</summary>
    ComposerConversionResult Convert(string content);
}
