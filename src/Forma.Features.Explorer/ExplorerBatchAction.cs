namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>A list of resources pending confirmation, replaced wholesale on each request.</summary>
public sealed class ExplorerBatchAction<TTarget> : ExplorerPendingAction
    where TTarget : ResourceInfo
{
    /// <summary>The resources currently pending, or empty when none are.</summary>
    public IReadOnlyList<TTarget> Targets { get; private set; } = [];

    /// <summary>Whether any resources are pending.</summary>
    public bool IsActive => Targets.Count > 0;

    /// <inheritdoc />
    public override bool CanConfirm => IsActive;

    /// <summary>Requests confirmation for the given resources, replacing any already pending.</summary>
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
