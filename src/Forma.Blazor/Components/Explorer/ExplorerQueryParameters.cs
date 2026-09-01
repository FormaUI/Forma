namespace Forma.Blazor.Components.Explorer;

using Forma.Features.Explorer;

/// <summary>The provider, folder, and page size driving the current query, watched together for any change.</summary>
internal sealed class ExplorerQueryParameters
{
    private readonly ParameterState<IExplorerResourcesProvider?> _provider = new();
    private readonly ParameterState<string?> _folderSource = new();
    private readonly ParameterState<int> _pageSize = new();

    /// <summary>Takes the current values, reporting whether any of them changed since they were last taken.</summary>
    public bool Update(IExplorerResourcesProvider? provider, string? folderSource, int pageSize)
    {
        var providerChanged = _provider.Update(provider);
        var folderChanged = _folderSource.Update(folderSource);
        var pageSizeChanged = _pageSize.Update(pageSize);

        return providerChanged | folderChanged | pageSizeChanged;
    }
}
