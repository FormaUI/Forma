namespace Forma.Blazor.Components.DataView;

using Microsoft.AspNetCore.Components;

/// <summary>The heading a view without rows draws over a group: its name, how many items it holds, and a chevron that folds it away.</summary>
/// <typeparam name="TItem">The item the group holds.</typeparam>
public sealed partial class FormaDataGroupHeading<TItem> : FormaComponentBase
    where TItem : notnull
{
    /// <summary>The group the heading stands over.</summary>
    [Parameter, EditorRequired] public DataViewGroup<TItem> Group { get; set; } = default!;

    /// <summary>The id the group's name carries, so the list under the heading can be named by it.</summary>
    [Parameter] public string? NameId { get; set; }

    /// <summary>Whether the group's items are folded away under the heading.</summary>
    [Parameter] public bool IsFolded { get; set; }

    /// <summary>Raised when the heading is pressed to fold the group away or open it again.</summary>
    [Parameter] public EventCallback OnToggle { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Group);
}
