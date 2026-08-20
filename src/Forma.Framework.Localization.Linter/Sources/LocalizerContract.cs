namespace Forma.Framework.Localization.Linter.Sources;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>Names the localizer contract the linter recognises.</summary>
public static class LocalizerContract
{
    /// <summary>The metadata name of the interface a localization lookup goes through.</summary>
    public const string MetadataName = "Forma.Framework.Localization.IStringLocale";

    /// <summary>The metadata name of the contract that names the type whose translations a lookup reads.</summary>
    public const string GenericMetadataName = "Forma.Framework.Localization.IStringLocale`1";

    /// <summary>The type name as source writes it, without namespace or type arguments.</summary>
    public const string TypeName = "IStringLocale";

    /// <summary>The type whose translations the receiver reads, or null when it names none.</summary>
    /// <param name="receiver">The type the lookup was made through.</param>
    /// <param name="generic">The generic contract as the compilation resolved it.</param>
    public static INamedTypeSymbol? ResourceOf(ITypeSymbol? receiver, INamedTypeSymbol generic)
    {
        ArgumentNullException.ThrowIfNull(generic);

        if (receiver is not INamedTypeSymbol named)
        {
            return null;
        }

        var closed = Closes(named, generic)
            ? named
            : named.AllInterfaces.FirstOrDefault(candidate => Closes(candidate, generic));

        // The factory reads an open definition, because a constructed generic spells its arguments into FullName.
        return closed?.TypeArguments[0] is INamedTypeSymbol resource ? resource.OriginalDefinition : null;
    }

    /// <summary>Whether the type is the localizer contract or implements it.</summary>
    /// <param name="type">The type to judge.</param>
    /// <param name="contract">The contract as the compilation resolved it.</param>
    public static bool Implements(INamedTypeSymbol? type, INamedTypeSymbol contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        if (type is null)
        {
            return false;
        }

        var unbound = type.IsGenericType ? type.OriginalDefinition : type;

        return SymbolEqualityComparer.Default.Equals(unbound, contract)
            || unbound.AllInterfaces.Any(candidate =>
                SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, contract));
    }

    /// <summary>Whether the type as written names the localizer contract.</summary>
    /// <param name="type">The type syntax to read.</param>
    public static bool Names(TypeSyntax? type)
    {
        var name = type switch
        {
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            _ => null,
        };

        return string.Equals(name, TypeName, StringComparison.Ordinal);
    }

    private static bool Closes(INamedTypeSymbol type, INamedTypeSymbol generic)
        => type.IsGenericType && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, generic);
}
