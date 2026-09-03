namespace Forma.Features.Explorer.Actions;

using Forma.Features.Explorer.Items;

/// <summary>An item and an editable name, held from Set until Clear.</summary>
public abstract class ExplorerNamedAction<TItem> : ExplorerPendingNamedAction
    where TItem : class, IExplorerItem
{
    private bool _isActive;
    private TItem? _item;

    /// <summary>The item the action is scoped to, or null when it has none.</summary>
    protected TItem? Item => _item;

    /// <inheritdoc />
    public override string Name { get; protected internal set; } = string.Empty;

    /// <summary>Whether the action is open.</summary>
    public bool IsActive => _isActive;

    /// <inheritdoc />
    public override bool CanConfirm => IsActive && !string.IsNullOrWhiteSpace(Name);

    /// <summary>The name as a confirmation would apply it.</summary>
    public string NameToConfirm => Name.Trim();

    /// <summary>Opens the action with the given item and starting name.</summary>
    protected void Set(TItem item, string initialName = "")
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(initialName);

        _item = item;

        Name = initialName;

        _isActive = true;
    }

    /// <summary>Opens the action with no item and an empty starting name.</summary>
    protected void Set()
    {
        _item = null;

        Name = string.Empty;

        _isActive = true;
    }

    /// <inheritdoc />
    protected internal override void Clear()
    {
        _item = null;

        Name = string.Empty;

        _isActive = false;
    }
}
