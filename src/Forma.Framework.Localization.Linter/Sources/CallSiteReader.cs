namespace Forma.Framework.Localization.Linter.Sources;

using System.Text;

using Forma.Framework.Localization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>Reads the key out of a lookup's arguments, mapped back to the file a human wrote.</summary>
internal static class CallSiteReader
{
    /// <summary>The name a lookup goes through when it is not an indexer.</summary>
    public const string ResolveMethod = "Resolve";

    /// <summary>The call site a lookup's arguments describe, or null when the key is not readable text.</summary>
    /// <param name="tree">The tree the arguments were parsed from.</param>
    /// <param name="arguments">The arguments the lookup was given.</param>
    /// <param name="symbol">The overload the lookup binds to, or null when nothing bound it.</param>
    /// <param name="model">The model that bound the arguments, or null when none did.</param>
    public static LocalizerCallSite? Read(SyntaxTree tree, BaseArgumentListSyntax arguments, ISymbol? symbol, SemanticModel? model)
    {
        var list = arguments.Arguments;

        return list.FirstOrDefault()?.Expression switch
        {
            LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression)
                => new LocalizerCallSite(
                    literal.Token.ValueText,
                    tree.GetMappedLineSpan(literal.Span),
                    Offset(literal),
                    Supplies(symbol, model, list),
                    list.Count - 1,
                    []),
            InterpolatedStringExpressionSyntax interpolated => Interpolated(tree, interpolated),
            _ => null,
        };
    }

    // Passing the collection itself rather than its items leaves nothing to count.
    private static bool Collection(ITypeSymbol argument, ITypeSymbol parameter, ITypeSymbol element)
        => SymbolEqualityComparer.Default.Equals(argument, parameter)
            || (argument is IArrayTypeSymbol array && SymbolEqualityComparer.Default.Equals(array.ElementType, element));

    private static ITypeSymbol? Element(ITypeSymbol type)
        => type switch
        {
            IArrayTypeSymbol array => array.ElementType,
            INamedTypeSymbol { TypeArguments.Length: 1 } named => named.TypeArguments[0],
            _ => null,
        };

    private static string Hole(StringBuilder key, InterpolationSyntax interpolation)
    {
        // The compiler passes the hole's text as written, which is the name a translation has to spell.
        var name = interpolation.Expression.ToString();

        key.Append('{').Append(name);

        if (interpolation.AlignmentClause is { Value: { } alignment } && alignment.ToString() != "0")
        {
            key.Append(',').Append(alignment.ToString());
        }

        if (interpolation.FormatClause is { } format)
        {
            key.Append(':').Append(format.FormatStringToken.ValueText);
        }

        key.Append('}');

        return name;
    }

    // The handler rebuilds the key from the parts, so reading it here has to reproduce that text.
    private static LocalizerCallSite Interpolated(SyntaxTree tree, InterpolatedStringExpressionSyntax interpolated)
    {
        var key = new StringBuilder();
        var holes = new List<string>();

        foreach (var content in interpolated.Contents)
        {
            switch (content)
            {
                case InterpolatedStringTextSyntax text:
                    Literal(key, text.TextToken.ValueText);

                    break;

                case InterpolationSyntax interpolation:
                    holes.Add(Hole(key, interpolation));

                    break;
            }
        }

        return new LocalizerCallSite(
            key.ToString(),
            tree.GetMappedLineSpan(interpolated.Span),
            0,
            LocalizerArguments.Interpolated,
            holes.Count,
            holes);
    }

    // A brace in prose is doubled so it survives the walk that later reads the key's holes.
    private static void Literal(StringBuilder key, string value)
    {
        foreach (var character in value)
        {
            if (character is '{' or '}')
            {
                key.Append(character);
            }

            key.Append(character);
        }
    }

    // The key's text starts after the opening quote, which a raw or verbatim literal makes more than one character.
    private static int Offset(LiteralExpressionSyntax literal)
        => literal.Token.Text.Length - literal.Token.Text.TrimStart('@', '$', '"').Length;

    private static LocalizerArguments Supplies(ISymbol? symbol, SemanticModel? model, SeparatedSyntaxList<ArgumentSyntax> list)
    {
        if (list.Count <= 1)
        {
            return LocalizerArguments.None;
        }

        var parameters = symbol switch
        {
            IPropertySymbol indexer => indexer.Parameters,
            IMethodSymbol method => method.Parameters,
            _ => default,
        };

        if (parameters.IsDefaultOrEmpty || Element(parameters[^1].Type) is not { } element)
        {
            return LocalizerArguments.Unknown;
        }

        if (list.Count == 2
            && model?.GetTypeInfo(list[1].Expression).Type is { } argument
            && Collection(argument, parameters[^1].Type, element))
        {
            return LocalizerArguments.Unknown;
        }

        return element switch
        {
            { Name: nameof(LocalizedArgument) } => LocalizerArguments.Named,
            { SpecialType: SpecialType.System_Object } => LocalizerArguments.Positional,
            _ => LocalizerArguments.Unknown,
        };
    }
}
