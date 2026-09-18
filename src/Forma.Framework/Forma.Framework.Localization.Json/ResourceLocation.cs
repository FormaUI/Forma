namespace Forma.Framework.Localization.Json;

using System.Reflection;

/// <summary>Where a type's translations are read from: the assembly, the name a lookup reports it searched, and the type's full name, which the files are laid out by.</summary>
/// <param name="Assembly">The assembly the type lives in.</param>
/// <param name="BaseName">The name a lookup reports it searched: the type's full name, qualified by the assembly's name for a type outside any namespace.</param>
/// <param name="TypeName">The type's full name.</param>
internal readonly record struct ResourceLocation(Assembly Assembly, string BaseName, string TypeName);
