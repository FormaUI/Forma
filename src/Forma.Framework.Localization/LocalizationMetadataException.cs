namespace Forma.Framework.Localization;

/// <summary>Thrown when a localization file's metadata is absent, malformed, or written for a schema this build cannot read.</summary>
public sealed class LocalizationMetadataException : Exception
{
    /// <summary>Creates an exception describing what is wrong with a file's metadata.</summary>
    /// <param name="message">What is wrong.</param>
    public LocalizationMetadataException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception describing what is wrong with a file's metadata.</summary>
    /// <param name="message">What is wrong.</param>
    /// <param name="innerException">The failure this one followed from.</param>
    public LocalizationMetadataException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
