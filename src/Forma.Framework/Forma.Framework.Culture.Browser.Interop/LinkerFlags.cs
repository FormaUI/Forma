namespace Forma.Framework.Culture.Browser.Interop;

using System.Diagnostics.CodeAnalysis;

/// <summary>The members the trimmer keeps for a type the JavaScript interop serializes, as Microsoft.JSInterop declares them.</summary>
internal static class LinkerFlags
{
    /// <summary>The constructors, fields and properties JSON serialization reads and writes.</summary>
    public const DynamicallyAccessedMemberTypes JsonSerialized = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties;
}
