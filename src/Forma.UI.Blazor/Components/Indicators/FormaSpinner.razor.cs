namespace Forma.UI.Blazor.Components.Indicators;

using Microsoft.AspNetCore.Components;

/// <summary>An indeterminate rotating ring announcing that something is loading.</summary>
public sealed partial class FormaSpinner : ComponentBase
{
    /// <summary>The accessible name announcing what is loading.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = "";

    /// <summary>Whether the label is also shown as visible text beside the ring.</summary>
    [Parameter] public bool ShowLabel { get; set; }

    /// <summary>The ring's size.</summary>
    [Parameter] public SpinnerSize Size { get; set; } = SpinnerSize.Medium;

    /// <summary>The value of the root element's id attribute.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Extra classes applied to the root element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Attributes applied to the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass
    {
        get
        {
            var sizeClass = Size switch
            {
                SpinnerSize.Small => "forma-spinner-small",
                SpinnerSize.Large => "forma-spinner-large",
                _ => "forma-spinner-medium",
            };

            return string.IsNullOrWhiteSpace(Class) ? $"forma-spinner {sizeClass}" : $"forma-spinner {sizeClass} {Class}";
        }
    }

    private string LabelClass
        => ShowLabel ? "forma-spinner-label" : "forma-spinner-label forma-spinner-visually-hidden";
}
