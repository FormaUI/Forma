namespace Forma.Framework.Localization;

/// <summary>Names a referenced assembly whose build embedded a host's locale files, which the runtime reads where it reads the host's folder.</summary>
/// <remarks>The localization package's targets write it into a Web App's server for each client it references whose build embedded a host's files, since the server renders the client's components too.</remarks>
/// <param name="assemblyName">The name of the assembly carrying the files.</param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class LocaleReferenceAttribute(string assemblyName) : Attribute
{
    /// <summary>The name of the assembly carrying the files.</summary>
    public string AssemblyName { get; } = assemblyName;
}
