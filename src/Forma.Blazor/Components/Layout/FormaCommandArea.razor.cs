namespace Forma.Blazor.Components.Layout;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A region holding as many command bars as a caller stacks into it.</summary>
public sealed partial class FormaCommandArea : FormaElementBase
{
    /// <summary>The area's bars, usually FormaCommandBar.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-command-area");
}
