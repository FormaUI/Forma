namespace Forma.Blazor.Components;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>Field chrome — the label, required marker and validation messages around a form element.</summary>
public sealed partial class FormaField : FormaElementBase
{
    /// <summary>The label text.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The id of the element the label points at.</summary>
    [Parameter] public string? TargetId { get; set; }

    /// <summary>Whether the label names the group of controls the field frames.</summary>
    [Parameter] public bool Grouped { get; set; }

    /// <summary>Whether the field is required, marked beside the label.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Whether the element is taller than one line, with the label anchored to its top.</summary>
    [Parameter] public bool Multiline { get; set; }

    /// <summary>Where the label sits relative to the element.</summary>
    [Parameter] public FieldLabelMode LabelMode { get; set; }

    /// <summary>The element the chrome wraps.</summary>
    /// <remarks>An element carrying the forma-field-input class takes the field's box, with the label floating over its value.</remarks>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>The model field whose validation messages render beneath the element.</summary>
    [Parameter] public FieldIdentifier? Field { get; set; }

    /// <summary>The shared ids of the field's label and validation-message container.</summary>
    [Parameter] public FieldElementIds? ElementIds { get; set; }

    [CascadingParameter] private EditContext? EditContext { get; set; }

    private EditContext? _observedEditContext;
    private FieldElementIds? _ownElementIds;

    private bool HasLabel
        => !string.IsNullOrWhiteSpace(Label);

    private string? GroupRole
        => Grouped ? "group" : null;

    private FieldElementIds EffectiveElementIds
        => ElementIds ?? (_ownElementIds ??= new FieldElementIds());

    private string? GroupLabelId
        => Grouped && HasLabel ? EffectiveElementIds.LabelId : null;

    private string? LabelModeClass
        => LabelMode switch
        {
            FieldLabelMode.Pinned => "forma-field-label-pinned",
            FieldLabelMode.Above => "forma-field-label-above",
            FieldLabelMode.Inline => "forma-field-label-inline",
            _ => null,
        };

    private IReadOnlyList<string> ValidationMessages
        => Field is { } identifier && EditContext is { } editContext ? [.. editContext.GetValidationMessages(identifier)] : [];

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (HasLabel && !Grouped)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(TargetId);
        }

        ArgumentNullException.ThrowIfNull(ChildContent);

        // The form reports validation through its edit context's event, which renders the messages again.
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
