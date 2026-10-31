# Forma.Analyzers

The Roslyn analyzers and the source generator Forma's own libraries build with. They run in the compiler and ship in no
package.

## Rules

| Rule | Reports |
| --- | --- |
| FORMA0001 | A named type crossing the JavaScript interop boundary without a type-level `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors \| DynamicallyAccessedMemberTypes.PublicProperties)]` |
| FORMA0002 | An anonymous type crossing the boundary, or passed anywhere as `object` |
| FORMA0003 | A type crossing the boundary without a public parameterless constructor |

A type crosses the boundary as an argument in an interop method's `object` array, as a type argument for an interop
type parameter that declares serialized members, such as an `InvokeAsync<TValue>` result, as a `[JSInvokable]`
method's parameter or result, and as a value an interop namespace returns as `object` or hands over from a method
returning one. Each is followed through its public properties, collections, nullable values and the types a
`[JsonDerivedType]` names. A type deriving from or implementing one of Forma's crossing types that no
`[JsonDerivedType]` names crosses wherever a value typed as that one does, serialized as itself, so it is checked where
it is declared. Every rule is an error, since what it reports fails only in a trimmed WebAssembly client.

## Generator

The trimmer applies a type-level `[DynamicallyAccessedMembers]` only where code asks a value typed as that type for its
type, and the interop serializes its values as `object`, so the annotation alone keeps nothing the interop reads.
`KeptMembersGenerator` makes it hold: for every type in the assembly that carries the annotation, or derives from a type
or implements an interface that does, and holds a member of the kinds it names, it emits a `[DynamicDependency]` naming
the same members on a module initializer that does nothing, which the trimmer keeps whenever it keeps the assembly. An
annotated abstract record with no property of its own is left out, since a dependency resolving no member fails the
trim, and what derives from it is kept by a dependency of its own.

## Use

Every library a WebAssembly client loads, each project the build marks `IsTrimmable`, references the project as an
analyzer through `Directory.Build.targets`.

## License

Apache License 2.0, under the repository's [`LICENSE`](../../LICENSE).
