namespace Forma.Blazor.Components.Layout;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A region holding a stack of command bars.</summary>
public sealed partial class FormaCommandArea : FormaElementBase
{
    /// <summary>The content the area stacks, its command bars.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-command-area");
}
