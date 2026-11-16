namespace Forma.Framework.Browser.Interop;

/// <summary>The browser entry modules the watcher and the engine are reached through.</summary>
internal static class DomRoots
{
    /// <summary>The watcher's entry, which observes.</summary>
    public static DomRoot Watcher { get; } = new("DomWatcher", "./_content/forma/browser/scripts/DomWatcher.js");

    /// <summary>The engine's entry, which acts.</summary>
    public static DomRoot Engine { get; } = new("DomEngine", "./_content/forma/browser/scripts/DomEngine.js");
}
