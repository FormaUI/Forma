namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Content.Queries;

/// <summary>The provider, folder, and page size driving the current query, watched together for any change.</summary>
internal sealed class ExplorerQueryParameters
{
    private readonly ParameterState<ICatalogProvider?> _provider = new();
    private readonly ParameterState<string?> _folderSource = new();
    private readonly ParameterState<int> _pageSize = new();

    /// <summary>Takes the current values, reporting whether any of them changed since they were last taken.</summary>
    public bool Update(ICatalogProvider? provider, string? folderSource, int pageSize)
    {
        var providerChanged = _provider.Update(provider);
        var folderChanged = _folderSource.Update(folderSource);
        var pageSizeChanged = _pageSize.Update(pageSize);

        return providerChanged | folderChanged | pageSizeChanged;
    }
}
