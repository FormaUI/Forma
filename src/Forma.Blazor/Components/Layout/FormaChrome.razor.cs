namespace Forma.Blazor.Components.Layout;

using Forma.Framework.Icons;
using Forma.Framework.Localization;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;

/// <summary>A frame with chrome: a title bar carrying the icon, the title and the host's commands as the frame's drag region, then an optional header band, the body, and an optional footer band. It is a region named by its title, and everything a frame is.</summary>
public sealed partial class FormaChrome : ComponentBase
{
    private readonly string _titleId = $"forma-chrome-title-{Guid.NewGuid():N}";

    [Inject] private IStringLocale<FormaChrome> L { get; set; } = default!;

    /// <summary>The title shown in the title bar and read as the chrome's name.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    /// <summary>The icon shown before the title.</summary>
    [Parameter] public Icon? Icon { get; set; }

    /// <summary>The host's commands, shown in the title bar after the title.</summary>
    [Parameter] public IReadOnlyList<FormaToolbarItem> Commands { get; set; } = [];

    /// <summary>Raised with the command of an item pressed in the title bar.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>The band beneath the title bar, generally text.</summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>The chrome's content.</summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>The band at the foot, generally actions or text.</summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>Where the frame stands and how large it is; unset, it stands in its layout.</summary>
    [Parameter] public FramePlacement? Placement { get; set; }

    /// <summary>Raised when the reader moves or resizes the frame, and once when it is first measured.</summary>
    [Parameter] public EventCallback<FramePlacement> PlacementChanged { get; set; }

    /// <summary>Whether the frame fills its bounds.</summary>
    [Parameter] public bool Maximized { get; set; }

    /// <summary>Whether the frame is held where it is: neither moved, resized nor maximized.</summary>
    [Parameter] public bool Locked { get; set; }

    /// <summary>What the reader may do to the frame, how far, and where it is held.</summary>
    [Parameter] public FrameOptions FrameOptions { get; set; } = FrameOptions.Default;

    /// <summary>Whether the frame is out of its presentation, its content kept.</summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>Whether the chrome is disabled: it takes no input and reads as inactive.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Additional attributes applied to the frame element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass
        => AdditionalAttributes.MergeClass("forma-chrome");

    // A host that gives the chrome another role names it itself.
    private string? LabelledBy
        => AdditionalAttributes.GetHostValue("role") is null ? _titleId : null;

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentException.ThrowIfNullOrWhiteSpace(Title);
}
