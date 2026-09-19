namespace Forma.Framework.Explorer.Tests.Items;

using Forma.Framework.Catalog;
using Forma.Framework.Explorer.Actions;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerItemManagerTests
{
    private static CatalogItem CreateItem(string source)
        => CatalogItem.For(new FileResourceInfo(source));

    private static CatalogFolderItem CreateFolderItem(string source)
        => CatalogItem.ForFolder(new FolderResourceInfo(source));

    private static CatalogItem CreateItem(string source, CatalogItemCapabilities capabilities, bool isDisabled = false)
    {
        var resource = new FileResourceInfo(source);

        return new CatalogItem { Key = source, Name = resource.Name, Capabilities = capabilities, IsDisabled = isDisabled, Resource = resource };
    }

    private static CatalogFolderItem CreateFolderItem(string source, CatalogItemCapabilities capabilities)
    {
        var folder = new FolderResourceInfo(source);

        return new CatalogFolderItem { Key = source, Name = folder.Name, Capabilities = capabilities, Resource = folder };
    }

    private static async Task<bool> TryDropAsync(ExplorerItemManager manager, ICatalogItem dragged, ICatalogItem? target)
    {
        var dropped = false;
        manager.OnDrop = (_, _) => { dropped = true; return Task.CompletedTask; };
        manager.BeginDrag(dragged);

        await manager.DropOnItemAsync(target);

        return dropped;
    }

    [Fact]
    public void Should_open_a_pending_rename_and_report_the_change()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var changes = 0;
        manager.OnChanged = () => changes++;

        // Act
        manager.RequestRename(CreateItem("/media/photo.png"));

        // Assert
        var rename = Assert.IsType<ExplorerRenameAction<ICatalogItem>>(manager.PendingAction);
        Assert.Multiple(
            () => Assert.Equal("/media/photo.png", rename.Target?.Key),
            () => Assert.Equal("photo.png", rename.Name),
            () => Assert.Equal(1, changes));
    }

    [Fact]
    public void Should_open_a_pending_rename_for_an_item_without_a_resource()
    {
        // Arrange
        var manager = new ExplorerItemManager();

        // Act
        manager.RequestRename(new CatalogItem { Key = "ghost", Name = "Ghost", Capabilities = CatalogItemCapabilities.Rename });

        // Assert
        var rename = Assert.IsType<ExplorerRenameAction<ICatalogItem>>(manager.PendingAction);
        Assert.Equal("ghost", rename.Target?.Key);
    }

    [Fact]
    public void Should_clear_the_pending_action_on_cancel_and_report_the_change()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        manager.RequestRename(CreateItem("/media/photo.png"));
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
        manager.RequestRename(CreateItem("/media/photo.png"));
        manager.SetActionName("  stage-one.png  ");

        ICatalogItem? applied = null;
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
            () => Assert.Equal("/media/photo.png", applied?.Key),
            () => Assert.Equal("stage-one.png", appliedName),
            () => Assert.False(pendingHeldDuringApply));
    }

    [Fact]
    public async Task Should_not_confirm_without_a_usable_name()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        manager.RequestRename(CreateItem("/media/photo.png"));
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
        var parent = CreateFolderItem("/photos");
        ICatalogFolderItem? appliedParent = null;
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
    public async Task Should_delete_the_selected_folders_before_the_selected_items()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var folder = CreateFolderItem("/photos");
        manager.SelectedItems = () => [folder, CreateItem("/media/photo.png")];
        IReadOnlyList<ICatalogFolderItem>? deleted = null;
        var itemsDeleted = false;
        manager.OnFolderDeleteConfirmed = folders => { deleted = folders; return Task.CompletedTask; };
        manager.OnDeleteConfirmed = _ => { itemsDeleted = true; return Task.CompletedTask; };

        // Act
        manager.RequestDeleteSelected();
        await manager.ConfirmActionAsync();

        // Assert
        Assert.Multiple(
            () => Assert.Same(folder, Assert.Single(deleted ?? [])),
            () => Assert.False(itemsDeleted));
    }

    [Fact]
    public async Task Should_delete_the_selected_items_when_no_folder_is_selected()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var item = CreateItem("/media/photo.png");
        manager.SelectedItems = () => [item];
        IReadOnlyList<ICatalogItem>? deleted = null;
        manager.OnDeleteConfirmed = items => { deleted = items; return Task.CompletedTask; };

        // Act
        manager.RequestDeleteSelected();
        await manager.ConfirmActionAsync();

        // Assert
        Assert.Same(item, Assert.Single(deleted ?? []));
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
    public void Should_open_a_pending_delete_for_a_single_item()
    {
        // Arrange
        var manager = new ExplorerItemManager();

        // Act
        manager.RequestDelete(CreateItem("/media/photo.png"));

        // Assert
        var deletion = Assert.IsType<ExplorerBatchAction<ICatalogItem>>(manager.PendingAction);
        Assert.Single(deletion.Targets);
    }

    [Fact]
    public void Should_open_a_folder_delete_when_the_single_item_is_a_folder()
    {
        // Arrange
        var manager = new ExplorerItemManager();

        // Act
        manager.RequestDelete(CreateFolderItem("/photos"));

        // Assert
        var deletion = Assert.IsType<ExplorerBatchAction<ICatalogFolderItem>>(manager.PendingAction);
        Assert.Single(deletion.Targets);
    }

    [Fact]
    public void Should_open_a_pending_delete_for_a_single_folder()
    {
        // Arrange
        var manager = new ExplorerItemManager();

        // Act
        manager.RequestFolderDelete(CreateFolderItem("/photos"));

        // Assert
        var deletion = Assert.IsType<ExplorerBatchAction<ICatalogFolderItem>>(manager.PendingAction);
        Assert.Single(deletion.Targets);
    }

    [Fact]
    public async Task Should_create_a_folder_at_the_top_level()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var applied = false;
        ICatalogFolderItem? appliedParent = CreateFolderItem("/leftover");
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
    public async Task Should_hand_the_dragged_item_to_the_drop_and_end_the_drag()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var dragged = CreateItem("/media/photo.png");
        var destination = CreateFolderItem("/photos");
        var drops = new List<(ICatalogItem Target, ICatalogFolderItem? Folder)>();
        manager.OnDrop = (target, folder) => { drops.Add((target, folder)); return Task.CompletedTask; };
        manager.BeginDrag(dragged);

        // Act
        await manager.DropOnItemAsync(destination);
        await manager.DropOnItemAsync(destination);

        // Assert
        var (target, folder) = Assert.Single(drops);
        Assert.Multiple(
            () => Assert.Same(dragged, target),
            () => Assert.Same(destination, folder));
    }

    [Fact]
    public async Task Should_ignore_a_drop_after_the_drag_is_ended()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var dropped = false;
        manager.OnDrop = (_, _) => { dropped = true; return Task.CompletedTask; };
        manager.BeginDrag(CreateItem("/media/photo.png"));

        // Act
        manager.EndDrag();
        await manager.DropOnItemAsync(CreateFolderItem("/photos"));

        // Assert
        Assert.False(dropped);
    }

    [Fact]
    public async Task Should_drop_onto_a_folder_that_has_no_resource()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var destination = new CatalogFolderItem { Key = "ghost", Name = "Ghost", Capabilities = CatalogItemCapabilities.Drop };
        ICatalogFolderItem? droppedOn = null;
        manager.OnDrop = (_, folder) => { droppedOn = folder; return Task.CompletedTask; };
        manager.BeginDrag(CreateItem("/media/photo.png"));

        // Act
        await manager.DropOnItemAsync(destination);

        // Assert
        Assert.Same(destination, droppedOn);
    }

    [Fact]
    public async Task Should_ignore_a_drop_onto_an_item_that_is_not_a_folder()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var dropped = false;
        manager.OnDrop = (_, _) => { dropped = true; return Task.CompletedTask; };
        manager.BeginDrag(CreateItem("/media/photo.png"));

        // Act
        await manager.DropOnItemAsync(CreateItem("/media/notes.md"));

        // Assert
        Assert.False(dropped);
    }

    [Fact]
    public async Task Should_not_begin_a_drag_of_an_item_that_cannot_be_dragged()
    {
        // Arrange
        var manager = new ExplorerItemManager();

        // Act
        var dropped = await TryDropAsync(manager, CreateItem("/media/photo.png", CatalogItemCapabilities.Move), CreateFolderItem("/photos"));

        // Assert
        Assert.False(dropped);
    }

    [Fact]
    public async Task Should_refuse_a_drop_of_an_item_that_cannot_move()
    {
        // Arrange
        var manager = new ExplorerItemManager();

        // Act
        var dropped = await TryDropAsync(manager, CreateItem("/media/photo.png", CatalogItemCapabilities.Drag), CreateFolderItem("/photos"));

        // Assert
        Assert.False(dropped);
    }

    [Fact]
    public async Task Should_refuse_a_drop_onto_a_folder_that_does_not_accept_drops()
    {
        // Arrange
        var manager = new ExplorerItemManager();

        // Act
        var dropped = await TryDropAsync(manager, CreateItem("/media/photo.png"), CreateFolderItem("/photos", CatalogItemCapabilities.Open));

        // Assert
        Assert.False(dropped);
    }

    [Fact]
    public async Task Should_refuse_a_drop_the_options_do_not_permit()
    {
        // Arrange
        var manager = new ExplorerItemManager
        {
            ItemOptions = new ExplorerItemOptions { AllowedCapabilities = CatalogItemCapabilities.Drag }
        };

        // Act
        var dropped = await TryDropAsync(manager, CreateItem("/media/photo.png"), CreateFolderItem("/photos"));

        // Assert
        Assert.False(dropped);
    }

    [Fact]
    public async Task Should_refuse_every_operation_on_a_disabled_item()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var item = CreateItem("/media/photo.png", CatalogItemCapabilities.Drag | CatalogItemCapabilities.Move | CatalogItemCapabilities.Rename | CatalogItemCapabilities.Delete, isDisabled: true);
        var activated = false;
        manager.OnActivate = _ => { activated = true; return Task.CompletedTask; };

        // Act
        await manager.ActivateAsync(item);
        manager.RequestRename(item);
        var renamePending = manager.PendingAction;
        manager.RequestDelete(item);
        var deletePending = manager.PendingAction;
        var dropped = await TryDropAsync(manager, item, CreateFolderItem("/photos"));

        // Assert
        Assert.Multiple(
            () => Assert.False(activated),
            () => Assert.Null(renamePending),
            () => Assert.Null(deletePending),
            () => Assert.False(dropped));
    }

    [Fact]
    public void Should_not_open_a_rename_for_an_item_that_cannot_be_renamed()
    {
        // Arrange
        var manager = new ExplorerItemManager();

        // Act
        manager.RequestRename(CreateItem("/media/photo.png", CatalogItemCapabilities.Delete));

        // Assert
        Assert.Null(manager.PendingAction);
    }

    [Fact]
    public void Should_not_open_a_delete_for_an_item_that_cannot_be_deleted()
    {
        // Arrange
        var manager = new ExplorerItemManager();

        // Act
        manager.RequestDelete(CreateItem("/media/photo.png", CatalogItemCapabilities.Rename));
        var itemPending = manager.PendingAction;
        manager.RequestFolderDelete(CreateFolderItem("/photos", CatalogItemCapabilities.Rename));
        var folderPending = manager.PendingAction;

        // Assert
        Assert.Multiple(
            () => Assert.Null(itemPending),
            () => Assert.Null(folderPending));
    }

    [Fact]
    public void Should_delete_only_the_selected_items_that_can_be_deleted()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        var deletable = CreateItem("/media/photo.png");
        manager.SelectedItems = () => [deletable, CreateItem("/media/fixed.png", CatalogItemCapabilities.Rename)];

        // Act
        manager.RequestDeleteSelected();

        // Assert
        var deletion = Assert.IsType<ExplorerBatchAction<ICatalogItem>>(manager.PendingAction);
        Assert.Same(deletable, Assert.Single(deletion.Targets));
    }

    [Fact]
    public void Should_open_nothing_when_no_selected_item_can_be_deleted()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        manager.SelectedItems = () => [CreateItem("/media/fixed.png", CatalogItemCapabilities.Rename)];

        // Act
        manager.RequestDeleteSelected();

        // Assert
        Assert.Null(manager.PendingAction);
    }

    [Fact]
    public void Should_not_create_a_folder_under_a_parent_that_does_not_accept_one()
    {
        // Arrange
        var manager = new ExplorerItemManager();

        // Act
        manager.BeginCreateFolder(CreateFolderItem("/photos", CatalogItemCapabilities.Open));

        // Assert
        Assert.Null(manager.PendingAction);
    }

    [Fact]
    public void Should_not_create_a_top_level_folder_the_options_do_not_permit()
    {
        // Arrange
        var manager = new ExplorerItemManager
        {
            ItemOptions = new ExplorerItemOptions { AllowedFolderCapabilities = CatalogItemCapabilities.Open }
        };

        // Act
        manager.BeginCreateFolder(null);

        // Assert
        Assert.Null(manager.PendingAction);
    }

    [Fact]
    public async Task Should_do_nothing_when_no_callbacks_are_wired()
    {
        // Arrange
        var manager = new ExplorerItemManager();
        manager.BeginDrag(CreateItem("/media/photo.png"));

        // Act & Assert
        manager.RequestRename(CreateItem("/media/photo.png"));
        manager.SetActionName("renamed.png");
        manager.CancelAction();
        manager.RequestDeleteSelected();
        manager.ToggleUpload();
        await manager.ActivateAsync(CreateItem("/media/photo.png"));
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
