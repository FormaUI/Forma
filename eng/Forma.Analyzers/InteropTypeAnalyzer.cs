namespace Forma.Analyzers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

/// <summary>Reports a type that crosses the JavaScript interop boundary in a shape its JSON serialization can't handle once trimmed.</summary>
/// <remarks>
/// The interop serializes its arguments and results by reflection, and the trimmer keeps nothing that only reflection
/// reads, so a type crossing the boundary declares what it keeps with a type-level <c>DynamicallyAccessedMembers</c>,
/// which <see cref="KeptMembersGenerator"/> keeps with the assembly, nested uses included. Trimming also removes constructors' parameter names, which
/// serialization matches a parameterized constructor by, so a type crossing it has a public parameterless constructor.
/// The boundary is crossed by an argument in an interop method's <c>object</c> array, by a type argument for an interop
/// type parameter that declares serialized members, by a <c>[JSInvokable]</c> method's parameters and result, by a value
/// an interop namespace returns as <c>object</c> or a method it hands over as a delegate returning one, and by an
/// anonymous object passed as <c>object</c>. A type is followed through its public properties, collections, nullable
/// values and the derived types it serializes as; a framework type serializes itself.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InteropTypeAnalyzer : DiagnosticAnalyzer
{
    /// <summary>A named type crossing the boundary without keeping the members its serialization uses.</summary>
    public static readonly DiagnosticDescriptor UnkeptType = new(
        "FORMA0001",
        "A type crossing the JavaScript interop boundary keeps its serialized members",
        "'{0}' crosses the JavaScript interop boundary{1}, but trimming removes the members its serialization uses; annotate it with [DynamicallyAccessedMembers(" + KeptMembers + ")]",
        "Trimming",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>An anonymous type crossing the boundary, which can't declare what it keeps.</summary>
    public static readonly DiagnosticDescriptor AnonymousType = new(
        "FORMA0002",
        "An anonymous type doesn't cross the JavaScript interop boundary",
        "An anonymous type crosses the JavaScript interop boundary{0}, and trimming removes its properties; declare a named type annotated with [DynamicallyAccessedMembers(" + KeptMembers + ")]",
        "Trimming",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>A type crossing the boundary that serialization would build through its constructor's parameter names.</summary>
    public static readonly DiagnosticDescriptor ConstructedByParameterNames = new(
        "FORMA0003",
        "A type crossing the JavaScript interop boundary has a public parameterless constructor",
        "'{0}' crosses the JavaScript interop boundary{1}, but its serialization matches its constructor by parameter names, which trimming removes; give it a public parameterless constructor and init properties",
        "Trimming",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private const string KeptMembers = "DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties";
    private const string KeptAttribute = "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute";
    private const string InvokableAttribute = "Microsoft.JSInterop.JSInvokableAttribute";
    private const string DerivedTypeAttribute = "System.Text.Json.Serialization.JsonDerivedTypeAttribute";
    private const int PublicConstructors = 0x0003;
    private const int PublicProperties = 0x0200;

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [UnkeptType, AnonymousType, ConstructedByParameterNames];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeConversion, OperationKind.Conversion);
        context.RegisterOperationAction(AnalyzeDelegateCreation, OperationKind.DelegateCreation);
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    // A type argument for an interop type parameter that declares serialized members, as an interop result's is.
    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        if (!IsInterop(method.ContainingNamespace))
        {
            return;
        }

        var definition = method.OriginalDefinition;

        for (var index = 0; index < definition.TypeParameters.Length; index++)
        {
            if (GetKeptMembers(definition.TypeParameters[index].GetAttributes()) is { } kept && (kept & PublicProperties) == PublicProperties)
            {
                Check(context.ReportDiagnostic, method.TypeArguments[index], invocation.Syntax.GetLocation());
            }
        }
    }

    // A value passed as an object is serialized by its runtime type: an anonymous one anywhere, and any other in an interop
    // argument or returned as an object in an interop namespace, as a capability describes its options for the browser.
    private static void AnalyzeConversion(OperationAnalysisContext context)
    {
        var conversion = (IConversionOperation)context.Operation;

        if (conversion.Type?.SpecialType != SpecialType.System_Object || conversion.Operand.Type is not { } type)
        {
            return;
        }

        if (HoldsAnonymousType(type))
        {
            context.ReportDiagnostic(Diagnostic.Create(AnonymousType, conversion.Operand.Syntax.GetLocation(), string.Empty));
        }
        else if (IsInteropArgument(conversion) || (conversion.Parent is IReturnOperation && IsInterop(context.ContainingSymbol.ContainingNamespace)))
        {
            Check(context.ReportDiagnostic, type, conversion.Operand.Syntax.GetLocation());
        }
    }

    // A method an interop namespace hands over as a delegate returning an object describes what the browser reads.
    private static void AnalyzeDelegateCreation(OperationAnalysisContext context)
    {
        var creation = (IDelegateCreationOperation)context.Operation;

        if (creation.Target is IMethodReferenceOperation { Method: var method }
            && creation.Type is INamedTypeSymbol { DelegateInvokeMethod.ReturnType.SpecialType: SpecialType.System_Object }
            && IsInterop(context.ContainingSymbol.ContainingNamespace))
        {
            if (HoldsAnonymousType(method.ReturnType))
            {
                context.ReportDiagnostic(Diagnostic.Create(AnonymousType, creation.Syntax.GetLocation(), string.Empty));
            }
            else
            {
                Check(context.ReportDiagnostic, method.ReturnType, creation.Syntax.GetLocation());
            }
        }
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (!method.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == InvokableAttribute))
        {
            return;
        }

        foreach (var parameter in method.Parameters)
        {
            Check(context.ReportDiagnostic, parameter.Type, parameter.Locations.FirstOrDefault() ?? Location.None);
        }

        var result = method.ReturnType is INamedTypeSymbol { IsGenericType: true } returned && returned.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks"
            ? returned.TypeArguments[0]
            : method.ReturnType;

        Check(context.ReportDiagnostic, result, method.Locations.FirstOrDefault() ?? Location.None);
    }

    private static bool IsInteropArgument(IConversionOperation conversion)
        => conversion.Parent is IArrayInitializerOperation { Parent: IArrayCreationOperation { Parent: IArgumentOperation { Parent: IInvocationOperation invocation } } }
            && IsInterop(invocation.TargetMethod.ContainingNamespace);

    private static bool HoldsAnonymousType(ITypeSymbol type)
        => type switch
        {
            IArrayTypeSymbol array => HoldsAnonymousType(array.ElementType),
            INamedTypeSymbol { IsAnonymousType: true } => true,
            INamedTypeSymbol named => named.TypeArguments.Any(HoldsAnonymousType),
            _ => false,
        };

    private static void Check(Action<Diagnostic> report, ITypeSymbol? type, Location location)
        => Check(report, type, location, path: string.Empty, [with(SymbolEqualityComparer.Default)]);

    private static void Check(Action<Diagnostic> report, ITypeSymbol? type, Location location, string path, HashSet<ITypeSymbol> visited)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                Check(report, array.ElementType, location, path, visited);
                return;

            case INamedTypeSymbol { IsAnonymousType: true }:
                report(Diagnostic.Create(AnonymousType, location, Where(path)));
                return;

            case INamedTypeSymbol named when named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T:
                Check(report, named.TypeArguments[0], location, path, visited);
                return;

            case INamedTypeSymbol named when named.SpecialType != SpecialType.None || named.TypeKind is TypeKind.Enum or TypeKind.Delegate:
                return;

            case INamedTypeSymbol named when IsFramework(named):
                // A framework collection serializes each element, and any other framework type serializes itself.
                if (IsCollection(named))
                {
                    foreach (var argument in named.TypeArguments)
                    {
                        Check(report, argument, location, path, visited);
                    }
                }

                return;

            case INamedTypeSymbol named:
                if (!visited.Add(named))
                {
                    return;
                }

                if ((GetKeptMembers(named) & (PublicConstructors | PublicProperties)) != (PublicConstructors | PublicProperties))
                {
                    report(Diagnostic.Create(UnkeptType, location, named.Name, Where(path)));
                }

                if (IsConstructedByParameterNames(named))
                {
                    report(Diagnostic.Create(ConstructedByParameterNames, location, named.Name, Where(path)));
                }

                foreach (var property in GetSerializedProperties(named))
                {
                    Check(report, property.Type, location, path.Length == 0 ? $"{named.Name}.{property.Name}" : $"{path}.{property.Name}", visited);
                }

                // A polymorphic type serializes as each type it names, which keeps its members through the base's annotation.
                foreach (var derived in GetDerivedTypes(named))
                {
                    Check(report, derived, location, path, visited);
                }

                return;
        }
    }

    // What the type, a base or an interface it implements keeps, since the trimmer applies a type's annotation to its derived types.
    private static int GetKeptMembers(INamedTypeSymbol type)
    {
        var kept = 0;

        for (var current = type; current is not null; current = current.BaseType)
        {
            kept |= GetKeptMembers(current.GetAttributes()) ?? 0;
        }

        foreach (var contract in type.AllInterfaces)
        {
            kept |= GetKeptMembers(contract.GetAttributes()) ?? 0;
        }

        return kept;
    }

    private static int? GetKeptMembers(ImmutableArray<AttributeData> attributes)
    {
        var attribute = attributes.FirstOrDefault(candidate => candidate.AttributeClass?.ToDisplayString() == KeptAttribute);

        return attribute is { ConstructorArguments.Length: 1 } && attribute.ConstructorArguments[0].Value is int kept ? kept : null;
    }

    // Serialization, writing as well as reading, configures a type without a public parameterless constructor through the
    // one it would construct it with, matching each parameter to a property by its name.
    private static bool IsConstructedByParameterNames(INamedTypeSymbol type)
        => !type.IsAbstract
            && type.TypeKind == TypeKind.Class
            && type.InstanceConstructors.Any(constructor => constructor.DeclaredAccessibility == Accessibility.Public && constructor.Parameters.Length > 0 && !IsCopyConstructor(type, constructor))
            && !type.InstanceConstructors.Any(constructor => constructor.DeclaredAccessibility == Accessibility.Public && constructor.Parameters.Length == 0);

    private static bool IsCopyConstructor(INamedTypeSymbol type, IMethodSymbol constructor)
        => constructor.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, type);

    private static IEnumerable<ITypeSymbol> GetDerivedTypes(INamedTypeSymbol type)
        => type.GetAttributes()
            .Where(attribute => attribute.AttributeClass?.ToDisplayString() == DerivedTypeAttribute && attribute.ConstructorArguments.Length > 0)
            .Select(attribute => attribute.ConstructorArguments[0].Value)
            .OfType<ITypeSymbol>();

    private static IEnumerable<IPropertySymbol> GetSerializedProperties(INamedTypeSymbol type)
    {
        for (var current = type; current is not null && !IsFramework(current); current = current.BaseType)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (property is { IsStatic: false, IsIndexer: false, DeclaredAccessibility: Accessibility.Public })
                {
                    yield return property;
                }
            }
        }
    }

    private static bool IsCollection(INamedTypeSymbol type)
        => type.AllInterfaces.Append(type).Any(contract => contract.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);

    private static bool IsFramework(INamedTypeSymbol type)
        => type.ContainingNamespace?.ToDisplayString() is { } name && (IsUnder(name, "System") || IsUnder(name, "Microsoft"));

    // Microsoft's interop, and every Forma namespace the interop wrappers live in.
    private static bool IsInterop(INamespaceSymbol? space)
        => space?.ToDisplayString() is { } name
            && (IsUnder(name, "Microsoft.JSInterop") || (IsUnder(name, "Forma") && name.Split('.').Contains("Interop")));

    private static bool IsUnder(string name, string root)
        => name == root || name.StartsWith(root + ".", StringComparison.Ordinal);

    private static string Where(string path)
        => path.Length == 0 ? string.Empty : $" as '{path}'";
}
