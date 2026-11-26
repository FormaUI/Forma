namespace Forma.Blazor.Components.DataView;

/// <summary>A command the reader ran from a narrow view's menu, and the selected items it acts on.</summary>
/// <typeparam name="TItem">The item the command acts on.</typeparam>
public sealed record DataViewCommandInvocation<TItem>
    where TItem : notnull
{
    /// <summary>The command run.</summary>
    public required DataViewCommand<TItem> Command { get; init; }

    /// <summary>The selected items the command acts on, one where it takes one.</summary>
    public required IReadOnlyList<TItem> Items { get; init; }
}
