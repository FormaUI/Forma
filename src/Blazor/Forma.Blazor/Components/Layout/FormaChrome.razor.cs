namespace Forma.Blazor.Components.Layout;

using Forma.Framework.Css;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;

/// <summary>A frame with chrome, named by its title: a title bar carrying the icon, the title and the commands as the frame's drag region, then a header band, the body and a footer band.</summary>
public sealed partial class FormaChrome : FormaElementBase
{
    /// <summary>The title shown in the title bar and read as the chrome's name.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    /// <summary>The icon shown before the title.</summary>
    [Parameter] public Icon? Icon { get; set; }

    /// <summary>The commands shown in the title bar after the title.</summary>
    [Parameter] public IReadOnlyList<ToolbarItem> Commands { get; set; } = [];

    /// <summary>Raised with the command of an item pressed in the title bar.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>The header band beneath the title bar.</summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>The chrome's content.</summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>The footer band at the chrome's foot.</summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>Where the frame stands and how large it is, where <see langword="null"/> stands it in its layout.</summary>
    [Parameter] public FramePlacement? Placement { get; set; }

    /// <summary>Raised when the reader moves or resizes the frame, and once when it is first measured.</summary>
    [Parameter] public EventCallback<FramePlacement> PlacementChanged { get; set; }

    /// <summary>Whether the frame fills its bounds.</summary>
    [Parameter] public bool Maximized { get; set; }

    /// <summary>Whether the frame is held at its placement and size.</summary>
    [Parameter] public bool Locked { get; set; }

    /// <summary>What the reader may do to the frame, how far, and where it is held.</summary>
    [Parameter] public FrameOptions FrameOptions { get; set; } = FrameOptions.Default;

    /// <summary>Whether the frame is hidden.</summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>Whether the chrome is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    private FormaFrame Frame { get; set; } = default!;

    /// <inheritdoc />
    public override ElementReference RootElement
        => Frame is null ? default : Frame.RootElement;

    private string TitleId
        => $"{ElementId}-title";

    private string? HostRole
        => AdditionalAttributes?.GetValueOrDefault("role")?.ToString();

    private string Role
        => HostRole ?? "region";

    // A host that gives the chrome another role names it itself.
    private string? LabelledBy
        => HostRole is null ? TitleId : null;

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentException.ThrowIfNullOrWhiteSpace(Title);

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-chrome");
}
