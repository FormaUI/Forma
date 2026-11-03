namespace Forma.Framework.Localization.Files;

/// <summary>Thrown when a <c>.locale.json</c> file cannot be read, or a key the runtime reads from it is malformed.</summary>
public sealed class LocaleConfigException : Exception
{
    /// <summary>Creates an exception describing what is wrong with a config file.</summary>
    /// <param name="message">What is wrong.</param>
    public LocaleConfigException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception describing what is wrong with a config file.</summary>
    /// <param name="message">What is wrong.</param>
    /// <param name="innerException">The failure this one followed from.</param>
    public LocaleConfigException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
