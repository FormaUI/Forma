namespace Forma.Framework.Explorer.Actions;

using Forma.Framework.Catalog;
using Forma.Framework.Explorer.Items;

/// <summary>A list of items pending confirmation, replaced wholesale on each request.</summary>
public sealed class ExplorerBatchAction<TTarget> : ExplorerPendingAction
    where TTarget : ICatalogItem
{
    /// <summary>The items currently pending, or empty when none are.</summary>
    public IReadOnlyList<TTarget> Targets { get; private set; } = [];

    /// <summary>Whether any items are pending.</summary>
    public bool IsActive => Targets.Count > 0;

    /// <inheritdoc />
    public override bool CanConfirm => IsActive;

    /// <summary>Requests confirmation for the given items, replacing any already pending.</summary>
    public void Request(IReadOnlyList<TTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);

        if (targets.Count == 0)
        {
            throw new ArgumentException("At least one target is required.", nameof(targets));
        }

        Targets = [.. targets];
    }

    /// <inheritdoc />
    protected internal override void Clear()
        => Targets = [];
}
