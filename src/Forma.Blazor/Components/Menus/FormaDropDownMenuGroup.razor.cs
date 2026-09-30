namespace Forma.Blazor.Components.Menus;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A group of items inside a <see cref="FormaDropDownMenu"/>.</summary>
public sealed partial class FormaDropDownMenuGroup : FormaElementBase
{
    /// <summary>The label heading the group.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The items in the group.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    private string LabelId
        => $"{ElementId}-label";

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-dropdown-menu-group");
}
