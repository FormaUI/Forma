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

    private static IEnumerable<LocalizerCallSite> Walk(Compilation compilation, SyntaxTree tree, INamedTypeSymbol contract)
    {
        var model = compilation.GetSemanticModel(tree);

        foreach (var node in tree.GetRoot().DescendantNodes())
        {
            BaseArgumentListSyntax? arguments = node switch
            {
                ElementAccessExpressionSyntax access when Owns(model, access, contract) => access.ArgumentList,
                InvocationExpressionSyntax invocation when Owns(model, invocation, contract) => invocation.ArgumentList,
                _ => null,
            };

            if (arguments is not null && CallSiteReader.Read(tree, arguments) is { } site)
            {
                yield return site;
            }
        }
    }

    private static bool Owns(SemanticModel model, SyntaxNode node, INamedTypeSymbol contract)
        => model.GetSymbolInfo(node).Symbol switch
        {
            IPropertySymbol indexer => LocalizerContract.Implements(indexer.ContainingType, contract),
            IMethodSymbol method => method.Name == CallSiteReader.ResolveMethod
                && LocalizerContract.Implements(method.ContainingType, contract),
            _ => false,
        };
}
