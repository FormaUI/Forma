namespace Forma.Framework.Localization;

/// <summary>Thrown when a key has no translation for the culture on screen and the host requires one.</summary>
public sealed class LocalizationNotFoundException : Exception
{
    /// <summary>Creates an exception naming the key that has no translation.</summary>
    /// <param name="message">What is missing.</param>
    public LocalizationNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception naming the key that has no translation.</summary>
    /// <param name="message">What is missing.</param>
    /// <param name="innerException">The failure this one followed from.</param>
    public LocalizationNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
