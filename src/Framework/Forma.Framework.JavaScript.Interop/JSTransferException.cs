namespace Forma.Framework.JavaScript.Interop;

/// <summary>The failure of a value the browser sent through the transfer, which its receiver applies nothing of.</summary>
public sealed class JSTransferException : Exception
{
    /// <summary>Creates the failure with no message of its own.</summary>
    public JSTransferException()
    {
    }

    /// <summary>Creates the failure with <paramref name="message"/>.</summary>
    /// <param name="message">What failed.</param>
    public JSTransferException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the failure with <paramref name="message"/>, caused by <paramref name="innerException"/>.</summary>
    /// <param name="message">What failed.</param>
    /// <param name="innerException">What caused it.</param>
    public JSTransferException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Whether the value was refused for its size, rather than lost on its way.</summary>
    public bool IsRefused { get; private init; }

    /// <summary>The failure of a value larger than its receiver accepts.</summary>
    internal static JSTransferException Refused(long length, long maximumLength)
        => new($"The browser sent {length} bytes where {maximumLength} are accepted.") { IsRefused = true };

    /// <summary>The failure of a stream that broke, stalled or ended short.</summary>
    internal static JSTransferException Broken(Exception cause)
        => new("The value the browser sent was lost on its way.", cause);
}
