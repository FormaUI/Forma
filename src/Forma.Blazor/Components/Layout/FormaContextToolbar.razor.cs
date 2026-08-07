namespace Forma.Blazor.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

/// <summary>A toolbar that appears while a selection is active, holding actions that apply to it.</summary>
public sealed partial class FormaContextToolbar : ComponentBase
{
    /// <summary>Number of items currently selected.</summary>
    [Parameter, EditorRequired] public int Count { get; set; }

    /// <summary>Text describing the active context; the selected-item count when omitted.</summary>
    [Parameter] public string? Caption { get; set; }

    /// <summary>Action buttons shown while the toolbar is active.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Raised when the clear-selection button is pressed.</summary>
    [Parameter] public EventCallback OnClear { get; set; }

    [Inject] private IStringLocalizer<FormaContextToolbar> L { get; set; } = default!;

    private string CaptionText
        => Caption ?? L["{0} selected", Count];

    protected override void OnParametersSet()
        => ArgumentOutOfRangeException.ThrowIfNegative(Count);
}
