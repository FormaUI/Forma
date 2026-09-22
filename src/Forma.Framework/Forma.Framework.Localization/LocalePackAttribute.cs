namespace Forma.Framework.Localization;

/// <summary>Names a locale pack an assembly was built against, which is the only record such a reference leaves: a pack declares no type, so nothing a host writes names one, the compiler records no reference to it and a trimmed publish would delete it. A pack's package carries a targets file that writes this attribute into the project referencing it.</summary>
/// <param name="assemblyName">The name of the pack's assembly.</param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class LocalePackAttribute(string assemblyName) : Attribute
{
    /// <summary>The name of the pack's assembly.</summary>
    public string AssemblyName { get; } = assemblyName;
}
