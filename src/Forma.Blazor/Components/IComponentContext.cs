namespace Forma.Blazor.Components;

using Forma.Framework.JavaScript.Interop;
using Forma.Framework.Localization;

using Microsoft.Extensions.Logging;

/// <summary>A component's context: its identity, its strings, its log and its JS environment.</summary>
public interface IComponentContext
{
    /// <summary>The component's identity for its lifetime.</summary>
    Guid RuntimeId { get; }

    /// <summary>The component's strings, in the reader's culture.</summary>
    IStringLocale Locale { get; }

    /// <summary>The component's log, under its own type as the category.</summary>
    ILogger Logger { get; }

    /// <summary>The JS environment the component's interop runs in: the runtime, under the component's own log and identity.</summary>
    IJSEnvironment JS { get; }
}
