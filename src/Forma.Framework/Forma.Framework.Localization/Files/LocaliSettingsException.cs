namespace Forma.Framework.Localization.Files;

/// <summary>Thrown when a <c>.locali.json</c> file cannot be read, or a key the runtime reads from it is malformed.</summary>
public sealed class LocaliSettingsException : Exception
{
    /// <summary>Creates an exception describing what is wrong with a settings file.</summary>
    /// <param name="message">What is wrong.</param>
    public LocaliSettingsException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception describing what is wrong with a settings file.</summary>
    /// <param name="message">What is wrong.</param>
    /// <param name="innerException">The failure this one followed from.</param>
    public LocaliSettingsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
