namespace Forma.UI.Blazor.Components.Explorer;

using Forma.Framework.Resources;

/// <summary>Request to rename a media resource.</summary>
/// <param name="Resource">The resource to rename.</param>
/// <param name="NewName">The new name requested for the resource.</param>
public sealed record ExplorerRenameRequest(ResourceInfo Resource, string NewName);

/// <summary>Request to delete one or more media resources.</summary>
/// <param name="Resources">The resources to delete.</param>
public sealed record ExplorerDeleteRequest(IReadOnlyList<ResourceInfo> Resources);

/// <summary>Request to move media resources into a target folder via drag-and-drop.</summary>
/// <param name="Resources">The resources being moved.</param>
/// <param name="TargetFolder">The destination folder receiving the resources, or null for the explorer root.</param>
public sealed record ExplorerDropRequest(IReadOnlyList<ResourceInfo> Resources, FolderResourceInfo? TargetFolder);

/// <summary>Request to move a folder into another parent folder via drag-and-drop.</summary>
/// <param name="SourceFolder">The folder being moved.</param>
/// <param name="TargetFolder">The destination parent folder, or null for the top level.</param>
public sealed record ExplorerFolderDropRequest(FolderResourceInfo SourceFolder, FolderResourceInfo? TargetFolder);

/// <summary>Request to create a new folder under an optional parent folder.</summary>
/// <param name="ParentFolder">The parent folder under which the new folder will be created, or null for top-level.</param>
/// <param name="Name">The display name of the new folder.</param>
public sealed record ExplorerFolderCreateRequest(FolderResourceInfo? ParentFolder, string Name);

/// <summary>Request to rename an existing folder.</summary>
/// <param name="Folder">The folder being renamed.</param>
/// <param name="NewName">The new display name for the folder.</param>
public sealed record ExplorerFolderRenameRequest(FolderResourceInfo Folder, string NewName);

/// <summary>Request to delete an existing folder.</summary>
/// <param name="Folder">The folder to delete.</param>
public sealed record ExplorerFolderDeleteRequest(FolderResourceInfo Folder);

/// <summary>Report that the explorer refused a resource, and why.</summary>
/// <param name="Reason">Why the explorer refused.</param>
/// <param name="Allowed">The ceiling that was reached, or null when the reason is not a count.</param>
/// <param name="Message">The reason, written for a reader in their own language.</param>
public sealed record ExplorerRejection(ExplorerRejectionReason Reason, long? Allowed, string Message);
