namespace Forma.Blazor.Components;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>Field chrome — the label, required marker and validation slot around a form element.</summary>
public sealed partial class FormaField : FormaElementBase
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

    private bool HasLabel
        => !string.IsNullOrWhiteSpace(Label);

    private string? LabelModeClass
        => LabelMode switch
        {
            FieldLabelMode.Pinned => "forma-field-label-pinned",
            FieldLabelMode.Above => "forma-field-label-above",
            FieldLabelMode.Inline => "forma-field-label-inline",
            _ => null,
        };

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (HasLabel)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(TargetId);
        }

        ArgumentNullException.ThrowIfNull(ChildContent);
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-field")
            .Add("forma-field-multiline", Multiline)
            .Add(LabelModeClass);
}
