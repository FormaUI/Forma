namespace Forma.Blazor.Components.Menus;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A named run of items inside a <see cref="FormaDropDownMenu"/>, headed by a label band that says what the items are.</summary>
public sealed partial class FormaDropDownMenuGroup : FormaElementBase
{
    /// <summary>The text heading the group; without one the group is unlabeled and draws no band.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The items in the group.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    private string LabelId
        => $"{ElementId}-label";

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-dropdown-menu-group");
}
