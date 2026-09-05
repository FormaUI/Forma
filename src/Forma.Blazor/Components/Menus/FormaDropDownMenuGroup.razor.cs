namespace Forma.Blazor.Components.Menus;

using Microsoft.AspNetCore.Components;

/// <summary>A named run of items inside a <see cref="FormaDropDownMenu"/>, headed by a label band that says what the items are.</summary>
public sealed partial class FormaDropDownMenuGroup : ComponentBase
{
    /// <summary>The text heading the group; without one the group is unlabeled and draws no band.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The items in the group.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>Additional attributes applied to the group element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private readonly string _labelId = $"ddm-group-{Guid.NewGuid():N}";
}
