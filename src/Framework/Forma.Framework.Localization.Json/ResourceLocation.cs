namespace Forma.Framework.Localization.Json;

using System.Reflection;

/// <summary>Where a type's entries are read from, one instance standing for each location a factory reads.</summary>
/// <param name="assembly">The assembly the type lives in.</param>
/// <param name="assemblyName">The assembly's name.</param>
/// <param name="baseName">The resource base name a lookup reports as searched.</param>
/// <param name="typeName">The type's full name, which the files are laid out by.</param>
internal sealed class ResourceLocation(Assembly assembly, string assemblyName, string baseName, string typeName)
{
    /// <summary>The assembly the type lives in.</summary>
    public Assembly Assembly
        => assembly;

    /// <summary>The assembly's name.</summary>
    public string AssemblyName
        => assemblyName;

    /// <summary>The resource base name a lookup reports as searched.</summary>
    public string BaseName
        => baseName;

    /// <summary>The type's full name, which the files are laid out by.</summary>
    public string TypeName
        => typeName;
}
