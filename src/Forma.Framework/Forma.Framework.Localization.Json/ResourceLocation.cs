namespace Forma.Framework.Localization.Json;

using System.Reflection;

/// <summary>Where a type's translations are read from: the assembly and the base name they are embedded under, and the type's full name, which the host's files are laid out by.</summary>
/// <param name="Assembly">The assembly the type lives in.</param>
/// <param name="BaseName">The name the type's files are embedded under, without the culture and extension.</param>
/// <param name="TypeName">The type's full name.</param>
internal readonly record struct ResourceLocation(Assembly Assembly, string BaseName, string TypeName);
