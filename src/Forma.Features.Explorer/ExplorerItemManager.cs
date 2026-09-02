namespace Forma.Features.Explorer;

/// <summary>Actions, pending confirmations, and drag state shared by every region an explorer renders its items in.</summary>
public sealed class ExplorerItemManager : IExplorerItemView, IExplorerItemInteraction
{
    private readonly ExplorerDragItem _drag = new();
    private readonly ExplorerRenameAction<IExplorerItem> _rename = new();
    private readonly ExplorerCreateFolderAction _createFolder = new();
    private readonly ExplorerBatchAction<IExplorerItem> _itemDeletion = new();
    private readonly ExplorerBatchAction<IExplorerFolderItem> _folderDeletion = new();

    /// <inheritdoc />
    public IReadOnlyList<ExplorerItemAction> Actions { get; set; } = [];

    /// <inheritdoc />
    public ExplorerItemOptions ItemOptions { get; set; } = ExplorerItemOptions.Default;

    /// <inheritdoc />
    public ExplorerPendingAction? PendingAction { get; private set; }

    /// <summary>Called after this manager's own state changes, so its owner can render.</summary>
    public Action? OnChanged { get; set; }

    /// <summary>Called to activate an item: preview it, or toggle its selection in selection mode.</summary>
    public Func<IExplorerItem, Task>? OnActivate { get; set; }

    /// <summary>Called to apply a drop of the given dragged item onto the given folder, or the top level when null.</summary>
    public Func<IExplorerItem, IExplorerFolderItem?, Task>? OnDrop { get; set; }

    /// <summary>Called to open the new-folder dialog under the current folder.</summary>
    public Func<Task>? OnRequestNewFolder { get; set; }

    /// <summary>Called to apply a confirmed rename.</summary>
    public Func<IExplorerItem, string, Task>? OnRenameConfirmed { get; set; }

    /// <summary>Called to apply a confirmed folder creation.</summary>
    public Func<IExplorerFolderItem?, string, Task>? OnCreateFolderConfirmed { get; set; }

    /// <summary>Called to apply a confirmed item deletion.</summary>
    public Func<IReadOnlyList<IExplorerItem>, Task>? OnDeleteConfirmed { get; set; }

    /// <summary>Called to apply a confirmed folder deletion.</summary>
    public Func<IReadOnlyList<IExplorerFolderItem>, Task>? OnFolderDeleteConfirmed { get; set; }

    /// <summary>Called to show or hide the upload panel.</summary>
    public Action? OnToggleUpload { get; set; }

    /// <summary>Supplies the currently selected items.</summary>
    public Func<IReadOnlyList<IExplorerItem>>? SelectedItems { get; set; }

    /// <inheritdoc />
    public Task ActivateAsync(IExplorerItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return OnActivate?.Invoke(item) ?? Task.CompletedTask;
    }

    /// <inheritdoc />
    public void BeginDrag(IExplorerItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _drag.Begin(item);
    }

    /// <inheritdoc />
    public Task DropOnItemAsync(IExplorerItem? target)
    {
        var dragged = _drag.Target;
        _drag.End();

        if (dragged is null)
        {
            return Task.CompletedTask;
        }

        // Only a folder can receive a drop; the null target is the top level.
        return target switch
        {
            null => OnDrop?.Invoke(dragged, null) ?? Task.CompletedTask,
            IExplorerFolderItem folder => OnDrop?.Invoke(dragged, folder) ?? Task.CompletedTask,
            _ => Task.CompletedTask,
        };
    }

    /// <inheritdoc />
    public void EndDrag()
        => _drag.End();

    /// <inheritdoc />
    public Task RequestNewFolderAsync()
        => OnRequestNewFolder?.Invoke() ?? Task.CompletedTask;

    /// <summary>Opens the new-folder dialog under the given parent, or the top level when null.</summary>
    public void BeginCreateFolder(IExplorerFolderItem? parent)
    {
        _createFolder.Request(parent);
        PendingAction = _createFolder;
        OnChanged?.Invoke();
    }

    /// <inheritdoc />
    public void RequestRename(IExplorerItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _rename.Request(item);
        PendingAction = _rename;
        OnChanged?.Invoke();
    }

    /// <inheritdoc />
    public void RequestDeleteSelected()
    {
        var items = SelectedItems?.Invoke() ?? [];

        if (items.Count == 0)
        {
            return;
        }

        List<IExplorerFolderItem> folders = [.. items.OfType<IExplorerFolderItem>()];

        if (folders.Count > 0)
        {
            _folderDeletion.Request(folders);
            PendingAction = _folderDeletion;
        }
        else
        {
            _itemDeletion.Request(items);
            PendingAction = _itemDeletion;
        }

        OnChanged?.Invoke();
    }

    /// <summary>Opens confirmation for deleting the single item, as a folder when it is one.</summary>
    public void RequestDelete(IExplorerItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item is IExplorerFolderItem folder)
        {
            RequestFolderDelete(folder);
            return;
        }

        _itemDeletion.Request([item]);
        PendingAction = _itemDeletion;
        OnChanged?.Invoke();
    }

    /// <summary>Opens confirmation for deleting the single folder.</summary>
    public void RequestFolderDelete(IExplorerFolderItem folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        _folderDeletion.Request([folder]);
        PendingAction = _folderDeletion;
        OnChanged?.Invoke();
    }

    /// <inheritdoc />
    public void SetActionName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (PendingAction is ExplorerPendingNamedAction action)
        {
            action.Name = name;
        }
    }

    /// <inheritdoc />
    public void CancelAction()
    {
        if (PendingAction is null)
        {
            return;
        }

        PendingAction.Clear();
        PendingAction = null;
        OnChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task ConfirmActionAsync()
    {
        if (PendingAction is not { CanConfirm: true } pending)
        {
            return;
        }

        PendingAction = null;

        switch (pending)
        {
            case ExplorerCreateFolderAction create:
                var parent = create.Parent;
                var folderName = create.NameToConfirm;
                create.Clear();
                await InvokeAsync(OnCreateFolderConfirmed, parent, folderName).ConfigureAwait(false);
                break;

            case ExplorerRenameAction<IExplorerItem> rename when rename.Target is { } target:
                var newName = rename.NameToConfirm;
                rename.Clear();
                await InvokeAsync(OnRenameConfirmed, target, newName).ConfigureAwait(false);
                break;

            case ExplorerBatchAction<IExplorerFolderItem> folders:
                var folderTargets = folders.Targets;
                folders.Clear();
                await InvokeAsync(OnFolderDeleteConfirmed, folderTargets).ConfigureAwait(false);
                break;

            case ExplorerBatchAction<IExplorerItem> items:
                var itemTargets = items.Targets;
                items.Clear();
                await InvokeAsync(OnDeleteConfirmed, itemTargets).ConfigureAwait(false);
                break;
        }
    }

    /// <inheritdoc />
    public void ToggleUpload()
        => OnToggleUpload?.Invoke();

    private static Task InvokeAsync<T>(Func<T, Task>? callback, T argument)
        => callback?.Invoke(argument) ?? Task.CompletedTask;

    private static Task InvokeAsync<T1, T2>(Func<T1, T2, Task>? callback, T1 first, T2 second)
        => callback?.Invoke(first, second) ?? Task.CompletedTask;
}
