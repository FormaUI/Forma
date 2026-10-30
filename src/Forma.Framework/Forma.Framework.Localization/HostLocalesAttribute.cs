namespace Forma.Framework.Localization;

/// <summary>Marks an assembly that carries a host's own locale files, embedded under <see cref="ResourcePrefix"/>, which are read where a host's folder is read.</summary>
/// <remarks>A WebAssembly client has no folder to read a host's files from, so the localization package's targets embed them in the client's assembly and write this attribute into it.</remarks>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class HostLocalesAttribute : Attribute
{
    /// <summary>What the resource name of each of the host's files starts with, followed by the file's path in the host's folder with its folders joined by dots; no resource name the build forms starts with a dot.</summary>
    public const string ResourcePrefix = ".locales.";
}
