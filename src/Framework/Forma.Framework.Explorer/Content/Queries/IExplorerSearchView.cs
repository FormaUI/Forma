namespace Forma.Framework.Explorer.Content.Queries;

using Forma.Framework.Catalog.Queries;

/// <summary>An explorer's search state, as the components drawing it read it.</summary>
public interface IExplorerSearchView
{
    /// <summary>The search query.</summary>
    string SearchQuery { get; }

    /// <summary>The search placeholder, or null for the default.</summary>
    string? SearchPlaceholder { get; }
}
