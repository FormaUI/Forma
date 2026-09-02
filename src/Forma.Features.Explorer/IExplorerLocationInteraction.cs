namespace Forma.Features.Explorer;

/// <summary>What the components drawing an explorer may ask its location to do.</summary>
public interface IExplorerLocationInteraction
{
    /// <summary>Returns to the previous folder.</summary>
    Task GoBackAsync();

    /// <summary>Returns to a folder left by going back.</summary>
    Task GoForwardAsync();

    /// <summary>Goes to the current folder's parent.</summary>
    Task GoUpAsync();

    /// <summary>Goes to the given folder, or the top level when null.</summary>
    Task SelectFolderAsync(IExplorerFolderItem? folder);
}
