namespace Forma.Blazor.Components;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>Collapses its content to nothing while keeping it mounted.</summary>
public sealed partial class FormaVisibility : FormaElementBase
{
    /// <summary>The content to show or collapse.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>Whether the content is collapsed and inert.</summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>Whether the transition between hidden and shown is animated.</summary>
    [Parameter] public bool Animated { get; set; }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-visibility")
            .Add("is-shown", !Hidden)
            .Add("is-animated", Animated);
}
