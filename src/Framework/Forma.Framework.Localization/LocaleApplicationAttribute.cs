namespace Forma.Framework.Localization;

/// <summary>Marks the assembly an application's build made, whose recorded locale packs and assemblies carrying a host's files the runtime reads where it names no entry assembly.</summary>
/// <remarks>The localization package's targets write it into a WebAssembly client, a server built on the Web SDK, and a Blazor Hybrid application.</remarks>
/// <param name="rootNamespace">The root namespace the application's build names its locale files under.</param>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class LocaleApplicationAttribute(string rootNamespace) : Attribute
{
    /// <summary>The root namespace the application's build names its locale files under.</summary>
    public string RootNamespace { get; } = rootNamespace;
}
