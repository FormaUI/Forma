namespace Forma.Framework.Browser.Interop.Operations;

/// <summary>Saves a file and shares an address natively, for a host whose web view does neither.</summary>
public interface INativeOperations
{
    /// <summary>Whether the host keeps a file through its own surface.</summary>
    bool CanSave { get; }

    /// <summary>Whether the host shares an address through its own share surface.</summary>
    bool CanShare { get; }

    /// <summary>Hands the reader text to keep as a file of the given name and media type, answering whether the host took it.</summary>
    /// <param name="name">The file's name.</param>
    /// <param name="content">The file's text.</param>
    /// <param name="type">The file's media type.</param>
    /// <returns>Whether the host took the file.</returns>
    Task<bool> TrySaveAsync(string name, string content, string type);

    /// <summary>Offers an address under a title to the host's share surface, answering whether the host took it.</summary>
    /// <param name="title">The title the address is shared under.</param>
    /// <param name="address">The address shared.</param>
    /// <returns>Whether the host took the address.</returns>
    Task<bool> TryShareAsync(string title, string address);
}
