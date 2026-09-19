namespace Forma.Framework.Localization.Files;

/// <summary>Thrown when a locale file cannot be read as one: its content is not what its format reads, or its metadata does not let it be read.</summary>
public class LocaleFileException : Exception
{
    /// <summary>Creates an exception describing what is wrong with a file.</summary>
    /// <param name="message">What is wrong.</param>
    public LocaleFileException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception describing what is wrong with a file.</summary>
    /// <param name="message">What is wrong.</param>
    /// <param name="innerException">The failure this one followed from.</param>
    public LocaleFileException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
