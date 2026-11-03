namespace Forma.Framework.Localization.Json.Tests.Fixtures;

/// <summary>A type with Hebrew and Spanish files in the <c>.locales</c> folder of its own folder, the default layout.</summary>
public sealed class Colocated;

/// <summary>A type with no locale file in any culture.</summary>
public sealed class Unlocalized;

/// <summary>A generic type, whose files are read for its name without the arity.</summary>
/// <typeparam name="TValue">The type parameter that makes the type generic.</typeparam>
public sealed class GenericArity<TValue>;

/// <summary>A type whose files are reached through the path a config file in another folder names.</summary>
public sealed class PathScoped;

/// <summary>A type whose localized string carries a plural expression.</summary>
public sealed class Pluralized;

/// <summary>A type whose file carries no metadata entry.</summary>
public sealed class Unmarked;

/// <summary>A type whose file carries a metadata entry that fails to parse as a field list.</summary>
public sealed class MalformedMetadata;

/// <summary>A type whose file is written for a later schema major version.</summary>
public sealed class FutureSchema;

/// <summary>A type whose file is written for a later schema minor version, carrying a field unknown to this build.</summary>
public sealed class NewerMinor;

/// <summary>A type whose file declares a culture other than the one its own name gives it.</summary>
public sealed class MismatchedCulture;

/// <summary>A type whose file gives a value of null, meaning the key itself is the localized string.</summary>
public sealed class NullValued;

/// <summary>A type with an embedded file whose entries the host's files override, one host file per key.</summary>
public sealed class HostOverridden;

/// <summary>A type localized by the host's files alone.</summary>
public sealed class HostLocalized;

/// <summary>A type with an embedded file whose entry a host file directly in the type's folder overrides, while the other entries stand.</summary>
public sealed class HostDotted;

/// <summary>A type whose host file carries no metadata entry.</summary>
public sealed class HostUnmarked;

/// <summary>A type whose host file gives a key a value other than text.</summary>
public sealed class HostMistyped;

/// <summary>A type localized by a host file named for it, directly in its folder.</summary>
public sealed class HostNamed;

/// <summary>A type localized by a host file in an unrelated folder that names the type from the root.</summary>
public sealed class HostRooted;

/// <summary>A type whose file carries its own strings at the top level and its nested types as sections.</summary>
public sealed class Outer
{
    /// <summary>A nested type, localized through its outer type's file or a file of its own.</summary>
    public sealed class Inner;

    /// <summary>A generic nested type, whose arity is dropped from its name.</summary>
    /// <typeparam name="TValue">The type parameter that makes the type generic.</typeparam>
    public sealed class InnerGeneric<TValue>;
}

/// <summary>A generic type whose arity is dropped from the name its nested type is reached through.</summary>
/// <typeparam name="TValue">The type parameter that makes the type generic.</typeparam>
public sealed class OuterGeneric<TValue>
{
    /// <summary>A type nested in a generic type.</summary>
    public sealed class Inner;
}

/// <summary>A type whose file has keys that start with a dollar sign, one escaped with a second and one plain text.</summary>
public sealed class Escaped;
