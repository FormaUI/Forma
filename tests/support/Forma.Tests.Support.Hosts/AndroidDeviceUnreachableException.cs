namespace Forma.Tests.Hosts;

/// <summary>The exception thrown where adb can't reach the device it addresses: offline, missing or unauthorized.</summary>
public sealed class AndroidDeviceUnreachableException : Exception
{
    /// <summary>Creates the exception.</summary>
    public AndroidDeviceUnreachableException()
    {
    }

    /// <summary>Creates the exception with <paramref name="message"/>.</summary>
    /// <param name="message">What adb said, and the devices it lists.</param>
    public AndroidDeviceUnreachableException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with <paramref name="message"/> and the exception that caused it.</summary>
    /// <param name="message">What adb said, and the devices it lists.</param>
    /// <param name="innerException">The exception that caused it.</param>
    public AndroidDeviceUnreachableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
