namespace Forma.Features.Explorer;

/// <summary>The configuration and capabilities exposed by an explorer.</summary>
public interface IExplorerConfigurationView
{
    /// <summary>How the explorer presents itself.</summary>
    ExplorerOptions Options { get; }

    /// <summary>Which operations the explorer offers on the resources it lists.</summary>
    ExplorerFileOptions FileOptions { get; }

    /// <summary>Whether the folder tree sidebar is shown.</summary>
    bool ShowsSidebar { get; }

    /// <summary>The operations offered on every resource, in render order.</summary>
    IReadOnlyList<ExplorerAction> Actions { get; }
}
