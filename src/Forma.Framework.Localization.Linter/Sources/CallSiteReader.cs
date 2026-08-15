namespace Forma.Framework.Localization.Linter.Sources;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>Reads the key out of a lookup's arguments, mapped back to the file a human wrote.</summary>
internal static class CallSiteReader
{
    /// <summary>The name a lookup goes through when it is not an indexer.</summary>
    public const string ResolveMethod = "Resolve";

    public static LocalizerCallSite? Read(SyntaxTree tree, BaseArgumentListSyntax arguments)
    {
        var argument = arguments.Arguments.FirstOrDefault()?.Expression;

        return argument switch
        {
            LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression)
                => new LocalizerCallSite(literal.Token.ValueText, tree.GetMappedLineSpan(literal.Span), Offset(literal)),
            InterpolatedStringExpressionSyntax interpolated
                => new LocalizerCallSite(null, tree.GetMappedLineSpan(interpolated.Span), 0),
            _ => null,
        };
    }

    // The key's text starts after the opening quote, which a raw or verbatim literal makes more than one character.
    private static int Offset(LiteralExpressionSyntax literal)
        => literal.Token.Text.Length - literal.Token.Text.TrimStart('@', '$', '"').Length;
}
