namespace Forma.Framework.Localization.Linter.Sources;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>Finds call sites by binding the receiver to the localizer contract.</summary>
public sealed class SemanticCallSiteFinder : ILocalizerCallSiteFinder
{
    /// <inheritdoc />
    public bool CanRead(Compilation compilation)
    {
        ArgumentNullException.ThrowIfNull(compilation);

        return compilation.GetTypeByMetadataName(LocalizerContract.MetadataName) is not null;
    }

    /// <inheritdoc />
    public IEnumerable<LocalizerCallSite> Find(Compilation compilation, SyntaxTree tree)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        ArgumentNullException.ThrowIfNull(tree);

        var contract = compilation.GetTypeByMetadataName(LocalizerContract.MetadataName);

        return contract is null ? [] : Walk(compilation, tree, contract);
    }

    private static bool Owns(ISymbol? symbol, INamedTypeSymbol contract)
        => symbol switch
        {
            IPropertySymbol indexer => LocalizerContract.Implements(indexer.ContainingType, contract),
            IMethodSymbol method => method.Name == CallSiteReader.ResolveMethod
                && LocalizerContract.Implements(method.ContainingType, contract),
            _ => false,
        };

    private static IEnumerable<LocalizerCallSite> Walk(Compilation compilation, SyntaxTree tree, INamedTypeSymbol contract)
    {
        var model = compilation.GetSemanticModel(tree);

        foreach (var node in tree.GetRoot().DescendantNodes())
        {
            var arguments = node switch
            {
                ElementAccessExpressionSyntax access => access.ArgumentList,
                InvocationExpressionSyntax invocation => (BaseArgumentListSyntax)invocation.ArgumentList,
                _ => null,
            };

            if (arguments is null)
            {
                continue;
            }

            var symbol = model.GetSymbolInfo(node).Symbol;

            if (Owns(symbol, contract) && CallSiteReader.Read(tree, arguments, symbol, model) is { } site)
            {
                yield return site;
            }
        }
    }
}
