namespace Forma.Features.Explorer;

/// <summary>Request to rename an item.</summary>
/// <param name="Item">The item to rename.</param>
/// <param name="NewName">The new name requested for the item.</param>
public sealed record ExplorerRenameRequest(IExplorerItem Item, string NewName);

/// <summary>Request to delete one or more items.</summary>
/// <param name="Items">The items to delete.</param>
public sealed record ExplorerDeleteRequest(IReadOnlyList<IExplorerItem> Items);

/// <summary>Request to move items into a target folder via drag-and-drop.</summary>
/// <param name="Items">The items being moved.</param>
/// <param name="TargetFolder">The destination folder receiving the items, or null for the explorer root.</param>
public sealed record ExplorerDropRequest(IReadOnlyList<IExplorerItem> Items, IExplorerFolderItem? TargetFolder);

/// <summary>Request to move a folder into another parent folder via drag-and-drop.</summary>
/// <param name="SourceFolder">The folder being moved.</param>
/// <param name="TargetFolder">The destination parent folder, or null for the top level.</param>
public sealed record ExplorerFolderDropRequest(IExplorerFolderItem SourceFolder, IExplorerFolderItem? TargetFolder);

/// <summary>Request to create a new folder under an optional parent folder.</summary>
/// <param name="ParentFolder">The parent folder under which the new folder will be created, or null for top-level.</param>
/// <param name="Name">The display name of the new folder.</param>
public sealed record ExplorerFolderCreateRequest(IExplorerFolderItem? ParentFolder, string Name);

/// <summary>Request to rename an existing folder.</summary>
/// <param name="Folder">The folder being renamed.</param>
/// <param name="NewName">The new display name for the folder.</param>
public sealed record ExplorerFolderRenameRequest(IExplorerFolderItem Folder, string NewName);

/// <summary>Request to delete an existing folder.</summary>
/// <param name="Folder">The folder to delete.</param>
public sealed record ExplorerFolderDeleteRequest(IExplorerFolderItem Folder);

/// <summary>Report that the explorer refused a resource, and why.</summary>
/// <param name="Reason">Why the explorer refused.</param>
/// <param name="Allowed">The ceiling that was reached, or null when the reason is not a count.</param>
/// <param name="Message">The reason, written for a reader in their own language.</param>
public sealed record ExplorerRejection(ExplorerRejectionReason Reason, long? Allowed, string Message);
