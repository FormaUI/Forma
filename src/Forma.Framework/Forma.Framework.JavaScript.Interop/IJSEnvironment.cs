namespace Forma.Framework.JavaScript.Interop;

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>A JS runtime carrying the identity and logger of the owner whose interop it runs.</summary>
public interface IJSEnvironment : IJSRuntime
{
    /// <summary>The runtime identity of the owner.</summary>
    Guid RuntimeId { get; }

    /// <summary>The owner's logger.</summary>
    ILogger Logger { get; }
}
