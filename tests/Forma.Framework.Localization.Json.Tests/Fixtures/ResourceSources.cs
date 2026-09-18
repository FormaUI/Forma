namespace Forma.Framework.Localization.Json.Tests.Fixtures;

/// <summary>Hebrew and Spanish files sitting beside the type, which is the default layout.</summary>
public sealed class Colocated;

/// <summary>No translation file in any culture.</summary>
public sealed class Unlocalized;

/// <summary>Generic type, to prove the arity is dropped from the file name.</summary>
/// <typeparam name="TValue">Unused; present only to make the type generic.</typeparam>
public sealed class GenericArity<TValue>;

/// <summary>Files reached through the configured resources folder rather than from beside the type.</summary>
public sealed class PathScoped;

/// <summary>A culture file kept out of a satellite, so it stays in the main assembly.</summary>
public sealed class MainAssembly;

/// <summary>A translation carrying a plural expression.</summary>
public sealed class Pluralized;

/// <summary>A file carrying no metadata entry at all.</summary>
public sealed class Unmarked;

/// <summary>A file whose metadata entry does not parse as a field list.</summary>
public sealed class MalformedMetadata;

/// <summary>A file written for a schema major this build does not read.</summary>
public sealed class FutureSchema;

/// <summary>A file written for a later minor schema, carrying a field this build does not know.</summary>
public sealed class NewerMinor;

/// <summary>A file declaring a culture other than the one its own name gives it.</summary>
public sealed class MismatchedCulture;

/// <summary>A file whose value is null, meaning the translation is deliberately the key.</summary>
public sealed class NullValued;

/// <summary>A file beside the type whose entries the host's files override, one file shape per key.</summary>
public sealed class HostOverridden;

/// <summary>No file beside the type; the host's files alone localize it.</summary>
public sealed class HostLocalized;

/// <summary>A file beside the type whose entry a host file in a <c>.locales</c> folder overrides, a form the assembly does not use.</summary>
public sealed class HostDotted;

/// <summary>A host file carrying no metadata entry.</summary>
public sealed class HostUnmarked;

/// <summary>A host file giving a key a value that is not text.</summary>
public sealed class HostMistyped;

/// <summary>A type whose file carries its own strings at the top level and its nested types as sections.</summary>
public sealed class Outer
{
    /// <summary>A nested type, translated through its outer type's file or a file of its own.</summary>
    public sealed class Inner;

    /// <summary>A generic nested type, whose arity is dropped like any other's.</summary>
    /// <typeparam name="TValue">Unused; present only to make the type generic.</typeparam>
    public sealed class InnerGeneric<TValue>;
}

/// <summary>A generic type whose arity is dropped from the name its nested type is reached through.</summary>
/// <typeparam name="TValue">Unused; present only to make the type generic.</typeparam>
public sealed class OuterGeneric<TValue>
{
    /// <summary>A type nested in a generic type.</summary>
    public sealed class Inner;
}

/// <summary>A file whose keys start with a dollar sign, one escaped with a second and one plain text.</summary>
public sealed class Escaped;
