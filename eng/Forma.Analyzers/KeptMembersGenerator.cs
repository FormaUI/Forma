namespace Forma.Analyzers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;

/// <summary>Keeps the members each type-level <c>DynamicallyAccessedMembers</c> annotation in an assembly names, on the annotated types and the types derived from them, whenever the trimmer keeps the assembly.</summary>
/// <remarks>
/// The trimmer applies a type's annotation only where code asks a value typed as that type for its type, and the JavaScript interop
/// serializes its values as <c>object</c>, so an annotation alone keeps nothing the interop reads. The generator turns each annotation
/// into a <c>DynamicDependency</c> on a module initializer that does nothing, which the trimmer keeps with its assembly, and names each
/// type by its documentation identifier, so a type nested out of reach of the initializer is named too.
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class KeptMembersGenerator : IIncrementalGenerator
{
    private const string KeptAttribute = "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var kept = context.CompilationProvider.Select(static (compilation, cancellationToken) => FindKept(compilation, cancellationToken));

        context.RegisterSourceOutput(kept, static (output, source) => Emit(output, source.Types, source.AssemblyName));
    }

    private static (EquatableArray Types, string AssemblyName) FindKept(Compilation compilation, CancellationToken cancellationToken)
    {
        var attribute = compilation.GetTypeByMetadataName(KeptAttribute);
        var types = new List<(string Id, int MemberTypes)>();

        if (attribute is not null)
        {
            foreach (var type in GetTypes(compilation.Assembly.GlobalNamespace))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (GetMemberTypes(type, attribute) is { } memberTypes && HoldsMembers(type, memberTypes) && type.GetDocumentationCommentId() is { } id)
                {
                    types.Add((id.Substring(2), memberTypes));
                }
            }
        }

        return (new EquatableArray([.. types.OrderBy(static type => type.Id, StringComparer.Ordinal)]), compilation.AssemblyName ?? string.Empty);
    }

    private static IEnumerable<INamedTypeSymbol> GetTypes(INamespaceOrTypeSymbol container)
    {
        foreach (var member in container.GetMembers())
        {
            if (member is INamespaceOrTypeSymbol nested)
            {
                if (nested is INamedTypeSymbol type)
                {
                    yield return type;
                }

                foreach (var inner in GetTypes(nested))
                {
                    yield return inner;
                }
            }
        }
    }

    // The members the type's own annotation names, with those its base types' and interfaces' annotations name, which reach it too.
    private static int? GetMemberTypes(INamedTypeSymbol type, INamedTypeSymbol attribute)
    {
        int? memberTypes = null;

        foreach (var annotated in new[] { type }.Concat(GetBaseTypes(type)).Concat(type.AllInterfaces))
        {
            foreach (var data in annotated.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(data.AttributeClass, attribute) && data.ConstructorArguments.FirstOrDefault().Value is int value)
                {
                    memberTypes = (memberTypes ?? 0) | value;
                }
            }
        }

        return memberTypes;
    }

    // A dependency naming no member a type has fails the trim, as one on an abstract record without properties would, and
    // what derives from the type is kept by dependencies of its own.
    private static bool HoldsMembers(INamedTypeSymbol type, int memberTypes)
    {
        const int PublicConstructors = 0x0003;
        const int PublicProperties = 0x0200;

        if ((memberTypes & ~(PublicConstructors | PublicProperties)) != 0)
        {
            return true;
        }

        return ((memberTypes & PublicConstructors) != 0 && type.InstanceConstructors.Any(static constructor => constructor.DeclaredAccessibility == Accessibility.Public))
            || ((memberTypes & PublicProperties) != 0 && new[] { type }.Concat(GetBaseTypes(type)).Any(static declaring => declaring.GetMembers().OfType<IPropertySymbol>().Any(static property => !property.IsStatic && property.DeclaredAccessibility == Accessibility.Public)));
    }

    private static IEnumerable<INamedTypeSymbol> GetBaseTypes(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            yield return current;
        }
    }

    private static void Emit(SourceProductionContext output, EquatableArray types, string assemblyName)
    {
        if (types.Items.IsEmpty)
        {
            return;
        }

        var source = new StringBuilder()
            .AppendLine("// <auto-generated/>")
            .AppendLine("namespace Forma.Generated")
            .AppendLine("{")
            .AppendLine("    /// <summary>Keeps the members each type-level annotation in the assembly names, whenever the trimmer keeps the assembly.</summary>")
            .AppendLine("    internal static class KeptMembers")
            .AppendLine("    {")
            .AppendLine("        [global::System.Runtime.CompilerServices.ModuleInitializer]");

        foreach (var (id, memberTypes) in types.Items)
        {
            source.AppendLine($"        [global::System.Diagnostics.CodeAnalysis.DynamicDependency((global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes){memberTypes}, \"{id}\", \"{assemblyName}\")]");
        }

        source
            .AppendLine("        internal static void Keep()")
            .AppendLine("        {")
            .AppendLine("        }")
            .AppendLine("    }")
            .AppendLine("}");

        output.AddSource("KeptMembers.g.cs", source.ToString());
    }

    /// <summary>The kept types, compared by their contents, so an edit that changes none of them generates nothing again.</summary>
    private readonly struct EquatableArray(ImmutableArray<(string Id, int MemberTypes)> items) : IEquatable<EquatableArray>
    {
        public ImmutableArray<(string Id, int MemberTypes)> Items { get; } = items;

        public bool Equals(EquatableArray other)
            => Items.SequenceEqual(other.Items);

        public override bool Equals(object? obj)
            => obj is EquatableArray other && Equals(other);

        public override int GetHashCode()
            => Items.Aggregate(17, static (hash, item) => (hash * 31) + item.GetHashCode());
    }
}
