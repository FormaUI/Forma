namespace Forma.Framework.Core.Interop;

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>The runtime a wrapper calls, carrying the identity and log of the owner whose interop it runs.</summary>
public interface IJSEnvironment : IJSRuntime
{
    /// <summary>The owner's identity for its lifetime.</summary>
    Guid RuntimeId { get; }

    /// <summary>The owner's log.</summary>
    ILogger Logger { get; }
}
