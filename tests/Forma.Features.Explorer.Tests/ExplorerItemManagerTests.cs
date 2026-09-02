namespace Forma.Features.Explorer.Tests;

using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerItemManagerTests
{
    private static ExplorerItem Item(string source)
        => ExplorerItem.For(new FileResourceInfo(source));

    private static ExplorerItem FolderItem(string source)
        => ExplorerItem.For(new FolderResourceInfo(source));

    [Fact]
    public void Should_open_a_pending_rename_and_report_the_change()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var changes = 0;
        manager.OnChanged = () => changes++;

        // Act
        manager.RequestRename(Item("/media/photo.png"));

        // Assert
        var rename = Assert.IsType<ExplorerRenameAction<ResourceInfo>>(manager.PendingAction);
        Assert.Multiple(
            () => Assert.Equal("/media/photo.png", rename.Target?.Source),
            () => Assert.Equal("photo.png", rename.Name),
            () => Assert.Equal(1, changes));
    }

    [Fact]
    public void Should_clear_the_pending_action_on_cancel_and_report_the_change()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        manager.RequestRename(Item("/media/photo.png"));
        var changes = 0;
        manager.OnChanged = () => changes++;

        // Act
        manager.CancelAction();

        // Assert
        Assert.Multiple(
            () => Assert.Null(manager.PendingAction),
            () => Assert.Equal(1, changes));
    }

    [Fact]
    public async Task Should_apply_the_trimmed_name_with_the_pending_action_already_cleared()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        manager.RequestRename(Item("/media/photo.png"));
        manager.SetActionName("  stage-one.png  ");

        ResourceInfo? applied = null;
        string? appliedName = null;
        var pendingHeldDuringApply = true;
        manager.OnRenameConfirmed = (target, newName) =>
        {
            applied = target;
            appliedName = newName;
            pendingHeldDuringApply = manager.PendingAction is not null;
            return Task.CompletedTask;
        };

        // Act
        await manager.ConfirmActionAsync();

        // Assert
        Assert.Multiple(
            () => Assert.Equal("/media/photo.png", applied?.Source),
            () => Assert.Equal("stage-one.png", appliedName),
            () => Assert.False(pendingHeldDuringApply));
    }

    [Fact]
    public async Task Should_not_confirm_without_a_usable_name()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        manager.RequestRename(Item("/media/photo.png"));
        manager.SetActionName("   ");
        var applied = false;
        manager.OnRenameConfirmed = (_, _) => { applied = true; return Task.CompletedTask; };

        // Act
        await manager.ConfirmActionAsync();

        // Assert
        Assert.Multiple(
            () => Assert.False(applied),
            () => Assert.NotNull(manager.PendingAction));
    }

    [Fact]
    public async Task Should_create_a_folder_under_the_given_parent()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var parent = new FolderResourceInfo("/photos");
        FolderResourceInfo? appliedParent = null;
        string? appliedName = null;
        manager.OnCreateFolderConfirmed = (target, name) =>
        {
            appliedParent = target;
            appliedName = name;
            return Task.CompletedTask;
        };

        // Act
        manager.BeginCreateFolder(parent);
        manager.SetActionName("Races");
        await manager.ConfirmActionAsync();

        // Assert
        Assert.Multiple(
            () => Assert.Same(parent, appliedParent),
            () => Assert.Equal("Races", appliedName),
            () => Assert.Null(manager.PendingAction));
    }

    [Fact]
    public async Task Should_delete_the_selected_folders_before_the_selected_resources()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var folder = new FolderResourceInfo("/photos");
        manager.SelectedItems = () => [ExplorerItem.For(folder), Item("/media/photo.png")];
        IReadOnlyList<FolderResourceInfo>? deleted = null;
        var resourcesDeleted = false;
        manager.OnFolderDeleteConfirmed = folders => { deleted = folders; return Task.CompletedTask; };
        manager.OnDeleteConfirmed = _ => { resourcesDeleted = true; return Task.CompletedTask; };

        // Act
        manager.RequestDeleteSelected();
        await manager.ConfirmActionAsync();

        // Assert
        Assert.Multiple(
            () => Assert.Same(folder, Assert.Single(deleted ?? [])),
            () => Assert.False(resourcesDeleted));
    }

    [Fact]
    public async Task Should_delete_the_selected_resources_when_no_folder_is_selected()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var resource = new FileResourceInfo("/media/photo.png");
        manager.SelectedItems = () => [ExplorerItem.For(resource)];
        IReadOnlyList<ResourceInfo>? deleted = null;
        manager.OnDeleteConfirmed = resources => { deleted = resources; return Task.CompletedTask; };

        // Act
        manager.RequestDeleteSelected();
        await manager.ConfirmActionAsync();

        // Assert
        Assert.Same(resource, Assert.Single(deleted ?? []));
    }

    [Fact]
    public void Should_open_nothing_when_nothing_is_selected()
    {
        // Arrange
        var manager = new ExplorerItemManager();

        // Act
        manager.RequestDeleteSelected();

        // Assert
        Assert.Null(manager.PendingAction);
    }

    [Fact]
    public void Should_open_a_pending_delete_for_a_single_resource()
    {
        // Arrange
        var manager = new ExplorerItemManager();

        // Act
        manager.RequestDelete(Item("/media/photo.png"));

        // Assert
        var deletion = Assert.IsType<ExplorerBatchAction<ResourceInfo>>(manager.PendingAction);
        Assert.Single(deletion.Targets);
    }

    [Fact]
    public void Should_open_a_pending_delete_for_a_single_folder()
    {
        // Arrange
        var manager = new ExplorerItemManager();

        // Act
        manager.RequestFolderDelete(new FolderResourceInfo("/photos"));

        // Assert
        var deletion = Assert.IsType<ExplorerBatchAction<FolderResourceInfo>>(manager.PendingAction);
        Assert.Single(deletion.Targets);
    }

    [Fact]
    public async Task Should_create_a_folder_at_the_top_level()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var applied = false;
        FolderResourceInfo? appliedParent = new FolderResourceInfo("/leftover");
        manager.OnCreateFolderConfirmed = (parent, _) =>
        {
            applied = true;
            appliedParent = parent;
            return Task.CompletedTask;
        };

        // Act
        manager.BeginCreateFolder(null);
        manager.SetActionName("Races");
        await manager.ConfirmActionAsync();

        // Assert
        Assert.Multiple(
            () => Assert.True(applied),
            () => Assert.Null(appliedParent));
    }

    [Fact]
    public async Task Should_hand_the_dragged_target_to_the_drop_and_end_the_drag()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var dragged = Item("/media/photo.png");
        var destination = FolderItem("/photos");
        var drops = new List<(ResourceInfo Target, FolderResourceInfo? Folder)>();
        manager.OnDrop = (target, folder) => { drops.Add((target, folder)); return Task.CompletedTask; };
        manager.BeginDrag(dragged);

        // Act
        await manager.DropOnItemAsync(destination);
        await manager.DropOnItemAsync(destination);

        // Assert
        var (target, folder) = Assert.Single(drops);
        Assert.Multiple(
            () => Assert.Same(dragged.Resource, target),
            () => Assert.Same(destination.Resource, folder));
    }

    [Fact]
    public async Task Should_ignore_a_drop_after_the_drag_is_ended()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var dropped = false;
        manager.OnDrop = (_, _) => { dropped = true; return Task.CompletedTask; };
        manager.BeginDrag(Item("/media/photo.png"));

        // Act
        manager.EndDrag();
        await manager.DropOnItemAsync(FolderItem("/photos"));

        // Assert
        Assert.False(dropped);
    }

    [Fact]
    public async Task Should_refuse_a_drop_onto_a_folder_the_host_cannot_address()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var dropped = false;
        manager.OnDrop = (_, _) => { dropped = true; return Task.CompletedTask; };
        manager.BeginDrag(Item("/media/photo.png"));

        // Act
        await manager.DropOnItemAsync(new ExplorerFolderItem { Key = "ghost", Name = "Ghost" });

        // Assert
        Assert.False(dropped);
    }

    [Fact]
    public async Task Should_ignore_a_drop_onto_an_item_that_is_not_a_folder()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var dropped = false;
        manager.OnDrop = (_, _) => { dropped = true; return Task.CompletedTask; };
        manager.BeginDrag(Item("/media/photo.png"));

        // Act
        await manager.DropOnItemAsync(Item("/media/notes.md"));

        // Assert
        Assert.False(dropped);
    }

    [Fact]
    public async Task Should_do_nothing_when_no_callbacks_are_wired()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        manager.BeginDrag(Item("/media/photo.png"));

        // Act & Assert
        manager.RequestRename(Item("/media/photo.png"));
        manager.SetActionName("renamed.png");
        manager.CancelAction();
        manager.RequestDeleteSelected();
        manager.ToggleUpload();
        await manager.ActivateAsync(Item("/media/photo.png"));
        await manager.DropOnItemAsync(null);
        await manager.RequestNewFolderAsync();
        await manager.ConfirmActionAsync();
    }

    [Fact]
    public async Task Should_reject_activating_a_null_item()
    {
        // Arrange
        var manager = new ExplorerItemManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.ActivateAsync(null!));
    }
}
