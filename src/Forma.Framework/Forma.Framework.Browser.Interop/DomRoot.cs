namespace Forma.Framework.Browser.Interop;

/// <summary>A browser entry module an owner's registrations and commands are made through.</summary>
/// <param name="Name">The entry's name, which diagnostics identify it by.</param>
/// <param name="Path">The entry's path under the content root.</param>
internal sealed record DomRoot(string Name, string Path)
{
    /// <summary>The watcher's entry, which observes.</summary>
    public static DomRoot Watcher { get; } = new("DomWatcher", "./_content/forma/browser/scripts/DomWatcher.js");

    /// <summary>The engine's entry, which acts.</summary>
    public static DomRoot Engine { get; } = new("DomEngine", "./_content/forma/browser/scripts/DomEngine.js");
}
