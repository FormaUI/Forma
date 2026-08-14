namespace Forma.Framework.Localization.Tests.Fixtures;

/// <summary>Hebrew and Spanish files sitting beside the type, which is the default layout.</summary>
public sealed class Colocated;

/// <summary>No translation file in any culture.</summary>
public sealed class Untranslated;

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
