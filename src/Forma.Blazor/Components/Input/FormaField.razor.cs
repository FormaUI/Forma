namespace Forma.Blazor.Components.Input;

using Microsoft.AspNetCore.Components;

/// <summary>Field chrome — the floating label, required marker and validation slot around an input element.</summary>
public sealed partial class FormaField : ComponentBase
{
    /// <summary>The floating label text.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Id of the input element the label points at.</summary>
    [Parameter, EditorRequired] public string FieldId { get; set; } = default!;

    /// <summary>Shows the required marker and is expected to accompany aria-required on the input element.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Anchors the label to the top of the field for an input element taller than one line.</summary>
    [Parameter] public bool Multiline { get; set; }

    /// <summary>The input element the chrome wraps, which must carry the forma-field-input class.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>Validation output for the input element, normally a ValidationMessage.</summary>
    [Parameter] public RenderFragment? Validation { get; set; }

    /// <summary>Additional attributes applied to the wrapping element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass
        => Multiline ? "forma-field forma-field-multiline" : "forma-field";

    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        ArgumentException.ThrowIfNullOrWhiteSpace(FieldId);
        ArgumentNullException.ThrowIfNull(ChildContent);
    }
}
