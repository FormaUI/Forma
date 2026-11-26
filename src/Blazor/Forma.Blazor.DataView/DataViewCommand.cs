namespace Forma.Blazor.Components.DataView;

using Forma.Framework.Icons;

/// <summary>A command a narrow view's menu offers on the selected items, enabled where it can act on every one of them.</summary>
/// <typeparam name="TItem">The item the command acts on.</typeparam>
public sealed record DataViewCommand<TItem>
    where TItem : notnull
{
    /// <summary>The command's identifier, unique among the view's commands.</summary>
    public required string Id { get; init; }

    /// <summary>The command's title, which the menu shows.</summary>
    public required string Title { get; init; }

    /// <summary>The command's icon, drawn before its title.</summary>
    public Icon? Icon { get; init; }

    /// <summary>Whether the command can act on an item, as one that can't be edited can't take Edit, where null lets it act on every item.</summary>
    public Func<TItem, bool>? AppliesTo { get; init; }

    /// <summary>Whether the command acts on several items at once, as Delete does, where false has it act on one, as Edit does.</summary>
    public bool TakesMany { get; init; }

    /// <summary>Whether the host disables the command, whatever is selected, where its context doesn't allow it.</summary>
    public bool IsDisabled { get; init; }

    /// <summary>Why the host disables the command, which the menu shows as its tooltip.</summary>
    public string? DisabledReason { get; init; }
}
