namespace Forma.Features.Explorer.Content.Queries;

/// <summary>An explorer's search state, as the components drawing it read it.</summary>
public interface IExplorerSearchView
{
    /// <summary>The text currently searched for.</summary>
    string SearchQuery { get; }

    /// <summary>The placeholder shown when nothing has been typed, or null for the default.</summary>
    string? SearchPlaceholder { get; }
}
