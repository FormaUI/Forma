namespace Forma.Blazor.Components;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>Collapses its content to nothing while keeping it mounted, so Context.JS-attached state on the content survives being hidden and shown again.</summary>
public sealed partial class FormaVisibility : FormaElementBase
{
    /// <summary>The content to show or collapse.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>Collapses the content and marks it inert instead of unmounting it.</summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>Animates the transition between hidden and shown instead of snapping instantly.</summary>
    [Parameter] public bool Animated { get; set; }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-visibility")
            .Add("is-shown", !Hidden)
            .Add("is-animated", Animated);
}
