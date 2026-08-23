namespace Forma.UI.Blazor.Components.Bars;

using Microsoft.AspNetCore.Components;

/// <summary>A region holding as many command bars as a caller stacks into it.</summary>
public sealed partial class FormaCommandArea : ComponentBase
{
    /// <summary>The area's bars, usually FormaCommandBar.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Extra classes applied alongside forma-command-area.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Additional attributes applied to the area element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass
        => string.IsNullOrWhiteSpace(Class) ? "forma-command-area" : $"forma-command-area {Class}";
}
