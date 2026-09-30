namespace Forma.Framework.Localization;

/// <summary>Names a locale pack an assembly was built against.</summary>
/// <remarks>A pack's package carries a targets file that writes this attribute into the project referencing it.</remarks>
/// <param name="assemblyName">The name of the pack's assembly.</param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class LocalePackAttribute(string assemblyName) : Attribute
{
    /// <summary>The name of the pack's assembly.</summary>
    public string AssemblyName { get; } = assemblyName;
}
