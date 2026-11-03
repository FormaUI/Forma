namespace Forma.Framework.Explorer;

/// <summary>The configuration and capabilities exposed by an explorer.</summary>
public interface IExplorerConfigurationView
{
    /// <summary>How the explorer presents itself.</summary>
    ExplorerOptions Options { get; }

    /// <summary>Whether the folder tree sidebar is shown.</summary>
    bool ShowsSidebar { get; }
}
