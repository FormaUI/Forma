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

/// <summary>A culture file backed by a neutral one holding the keys it omits.</summary>
public sealed class NeutralBacked;

/// <summary>A culture file kept out of a satellite, so it stays in the main assembly.</summary>
public sealed class MainAssembly;
