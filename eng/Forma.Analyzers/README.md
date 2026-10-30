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
`[JsonDerivedType]` names. Every rule is an error, since what it reports fails only in a trimmed WebAssembly client.

## Generator

The trimmer applies a type-level `[DynamicallyAccessedMembers]` only where code asks a value typed as that type for its
type, and the interop serializes its values as `object`, so the annotation alone keeps nothing the interop reads.
`KeptMembersGenerator` makes it hold: for every type in the assembly that carries the annotation, or derives from a type
or implements an interface that does, it emits a `[DynamicDependency]` naming the same members on a module initializer
that does nothing, which the trimmer keeps whenever it keeps the assembly.

## Use

Every library a WebAssembly client loads, each project the build marks `IsTrimmable`, references the project as an
analyzer through `Directory.Build.targets`.

## License

Apache License 2.0, under the repository's [`LICENSE`](../../LICENSE).
