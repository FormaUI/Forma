# Forma.Analyzers

The Roslyn analyzers Forma's own libraries build with. They run in the compiler and ship in no package.

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

## Use

Every library a WebAssembly client loads, each project the build marks `IsTrimmable`, references the project as an
analyzer through `Directory.Build.targets`.

## License

Apache License 2.0, under the repository's [`LICENSE`](../../LICENSE).
