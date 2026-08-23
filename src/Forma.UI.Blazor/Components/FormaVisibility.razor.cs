namespace Forma.UI.Blazor.Components;

using Microsoft.AspNetCore.Components;

/// <summary>Collapses its content to nothing while keeping it mounted, so JS-attached state on the content survives being hidden and shown again.</summary>
public sealed partial class FormaVisibility : ComponentBase
{
    /// <summary>The content to show or collapse.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>Collapses the content and marks it inert instead of unmounting it.</summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>Animates the transition between hidden and shown instead of snapping instantly.</summary>
    [Parameter] public bool Animated { get; set; }

    private string CssClass
    {
        get
        {
            var classes = "forma-visibility";

            if (!Hidden)
            {
                classes += " is-shown";
            }

            if (Animated)
            {
                classes += " is-animated";
            }

            return classes;
        }
    }
}
