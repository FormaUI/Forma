namespace Forma.UI.Blazor.Components;

using Microsoft.AspNetCore.Components;

/// <summary>Field chrome — the label, required marker and validation slot around a form element.</summary>
public sealed partial class FormaField : ComponentBase
{
    /// <summary>The label text, absent which no label renders.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Id of the element the label points at.</summary>
    [Parameter] public string? TargetId { get; set; }

    /// <summary>Shows the required marker beside the label.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Anchors the label to the top for an element taller than one line.</summary>
    [Parameter] public bool Multiline { get; set; }

    /// <summary>Where the label sits relative to the element.</summary>
    [Parameter] public FieldLabelMode LabelMode { get; set; }

    /// <summary>The element the chrome wraps; the forma-field-input class opts it into the field's box.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>Validation output for the element, normally a ValidationMessage.</summary>
    [Parameter] public RenderFragment? Validation { get; set; }

    /// <summary>Additional attributes applied to the wrapping element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private bool HasLabel
        => !string.IsNullOrWhiteSpace(Label);

    private string CssClass
    {
        get
        {
            var classes = "forma-field";

            if (Multiline)
            {
                classes += " forma-field-multiline";
            }

            classes += LabelMode switch
            {
                FieldLabelMode.Pinned => " forma-field-label-pinned",
                FieldLabelMode.Above => " forma-field-label-above",
                FieldLabelMode.Inline => " forma-field-label-inline",
                _ => string.Empty,
            };

            // Merged rather than left to the splat, which the explicit class attribute would override.
            var hostClass = AdditionalAttributes is not null && AdditionalAttributes.TryGetValue("class", out var value)
                ? value?.ToString()
                : null;

            return string.IsNullOrWhiteSpace(hostClass) ? classes : $"{classes} {hostClass}";
        }
    }

    protected override void OnParametersSet()
    {
        if (HasLabel)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(TargetId);
        }

        ArgumentNullException.ThrowIfNull(ChildContent);
    }
}
