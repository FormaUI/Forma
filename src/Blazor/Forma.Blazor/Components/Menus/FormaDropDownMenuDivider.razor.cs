namespace Forma.Blazor.Components.Menus;

using Forma.Framework.Css;

/// <summary>A visual separator between groups of items inside a <see cref="FormaDropDownMenu"/>.</summary>
public sealed partial class FormaDropDownMenuDivider : FormaElementBase
{
    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-dropdown-menu-divider");
}
