namespace Forma.Blazor.Components.Input;

using Microsoft.AspNetCore.Components;

/// <summary>Field chrome — the floating label, required marker and validation slot around an input element.</summary>
public sealed partial class FormaField : ComponentBase
{
    /// <summary>The label text. Without one no label renders, leaving the control to carry its own accessible name.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Id of the input element the label points at, required whenever a label renders.</summary>
    [Parameter] public string? FieldId { get; set; }

    /// <summary>Shows the required marker and is expected to accompany aria-required on the input element.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Anchors the label to the top of the field for an input element taller than one line.</summary>
    [Parameter] public bool Multiline { get; set; }

    /// <summary>Where the label sits relative to the control.</summary>
    [Parameter] public FieldLabelMode LabelMode { get; set; }

    /// <summary>The input element the chrome wraps, which must carry the forma-field-input class.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>Validation output for the input element, normally a ValidationMessage.</summary>
    [Parameter] public RenderFragment? Validation { get; set; }

    /// <summary>Additional attributes applied to the wrapping element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private bool HasLabel => !string.IsNullOrWhiteSpace(Label);

    private string CssClass
    {
        get
        {
            var classes = "forma-field";

            if (Multiline)
            {
                classes += " forma-field-multiline";
            }

            return classes + (LabelMode switch
            {
                FieldLabelMode.Pinned => " forma-field-label-pinned",
                FieldLabelMode.Above => " forma-field-label-above",
                FieldLabelMode.Inline => " forma-field-label-inline",
                _ => string.Empty,
            });
        }
    }

    protected override void OnParametersSet()
    {
        if (HasLabel)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(FieldId);
        }

        ArgumentNullException.ThrowIfNull(ChildContent);
    }
}
