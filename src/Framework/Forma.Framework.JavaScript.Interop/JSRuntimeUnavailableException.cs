namespace Forma.Framework.JavaScript.Interop;

/// <summary>The exception for invoking JavaScript where no runtime is available.</summary>
public sealed class JSRuntimeUnavailableException : InvalidOperationException
{
    /// <summary>Creates the exception with its default message.</summary>
    public JSRuntimeUnavailableException()
        : base("No JavaScript runtime is available.")
    {
    }

    /// <summary>Creates the exception with the given message.</summary>
    public JSRuntimeUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with the given message and the exception that caused it.</summary>
    public JSRuntimeUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
