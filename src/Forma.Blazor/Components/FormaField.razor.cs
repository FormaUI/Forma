namespace Forma.Blazor.Components;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>Field chrome — the label, required marker and validation messages around a form element.</summary>
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

    /// <summary>The model field whose validation messages render beneath the element; absent, none render.</summary>
    [Parameter] public FieldIdentifier? Field { get; set; }

    [CascadingParameter] private EditContext? EditContext { get; set; }

    private EditContext? _observedEditContext;

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

    private IEnumerable<string> ValidationMessages
        => Field is { } identifier && EditContext is { } editContext ? editContext.GetValidationMessages(identifier) : [];

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (HasLabel)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(TargetId);
        }

        ArgumentNullException.ThrowIfNull(ChildContent);

        // The messages change when the form validates, which the form reports through its context rather than through a parameter.
        if (!ReferenceEquals(EditContext, _observedEditContext))
        {
            StopObservingValidation();
            _observedEditContext = EditContext;
            _observedEditContext?.OnValidationStateChanged += HandleValidationStateChanged;
        }
    }

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs args)
        => StateHasChanged();

    private void StopObservingValidation()
    {
        _observedEditContext?.OnValidationStateChanged -= HandleValidationStateChanged;
        _observedEditContext = null;
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-field")
            .Add("forma-field-multiline", Multiline)
            .Add(LabelModeClass);

    /// <inheritdoc />
    protected override ValueTask DisposeAsyncCore()
    {
        StopObservingValidation();

        return base.DisposeAsyncCore();
    }
}
