namespace Forma.Framework.Localization.Json;

using System.Reflection;

/// <summary>Where a type's translations are read from.</summary>
/// <param name="Assembly">The assembly the type lives in.</param>
/// <param name="BaseName">The resource base name a lookup reports as searched.</param>
/// <param name="TypeName">The type's full name, which the files are laid out by.</param>
internal readonly record struct ResourceLocation(Assembly Assembly, string BaseName, string TypeName);
