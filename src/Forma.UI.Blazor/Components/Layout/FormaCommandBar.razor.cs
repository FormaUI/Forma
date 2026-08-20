namespace Forma.UI.Blazor.Components.Layout;

using Microsoft.AspNetCore.Components;

/// <summary>A region holding as many command rows as a caller stacks into it.</summary>
public sealed partial class FormaCommandBar : ComponentBase
{
    /// <summary>The bar's rows, usually FormaCommandBarRow.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Extra classes applied alongside forma-command-bar.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Additional attributes applied to the bar element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass
        => string.IsNullOrWhiteSpace(Class) ? "forma-command-bar" : $"forma-command-bar {Class}";
}
