namespace Forma.Blazor.Components;

using Forma.Framework.Browser.Interop;
using Forma.Framework.Culture;
using Forma.Framework.JavaScript.Interop;
using Forma.Framework.Localization;

using Microsoft.Extensions.Logging;

/// <summary>A component's context: its identity, its cultures, its strings, its log, its JS environment, and the watcher and engine its browser work goes through.</summary>
public interface IComponentContext
{
    /// <summary>The component's identity for its lifetime.</summary>
    Guid RuntimeId { get; }

    /// <summary>The cultures the component writes its strings, formats and direction in.</summary>
    ICultureState CultureState { get; }

    /// <summary>The component's strings, in the reader's culture.</summary>
    IStringLocale Locale { get; }

    /// <summary>The component's log, under its own type as the category.</summary>
    ILogger Logger { get; }

    /// <summary>The JS environment the component's interop runs in: the runtime, under the component's own log and identity.</summary>
    IJSEnvironment JS { get; }

    /// <summary>What the component observes of its elements.</summary>
    DomWatcher DomWatcher { get; }

    /// <summary>What the component does to its elements, through its watcher.</summary>
    DomEngine DomEngine { get; }
}
