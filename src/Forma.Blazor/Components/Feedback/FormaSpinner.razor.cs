namespace Forma.Blazor.Components.Feedback;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>An indeterminate rotating ring announcing that something is loading.</summary>
public sealed partial class FormaSpinner : FormaElementBase
{
    /// <summary>The accessible name announcing what is loading.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = "";

    /// <summary>Whether the label is also shown as visible text beside the ring.</summary>
    [Parameter] public bool ShowLabel { get; set; }

    /// <summary>The ring's size.</summary>
    [Parameter] public SpinnerSize Size { get; set; } = SpinnerSize.Medium;

    private string SizeClass
        => Size switch
        {
            SpinnerSize.Small => "forma-spinner-small",
            SpinnerSize.Large => "forma-spinner-large",
            _ => "forma-spinner-medium",
        };

    private string LabelClass
        => ShowLabel ? "forma-spinner-label" : "forma-spinner-label forma-spinner-visually-hidden";

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-spinner")
            .Add(SizeClass);
}
