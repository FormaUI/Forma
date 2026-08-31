namespace Forma.Blazor.Components.Feedback;

using Forma.Features.Feedback;
using Forma.Framework.Css;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;

/// <summary>A pill-shaped label showing the status of a record.</summary>
public sealed partial class FormaStatusBadge : ComponentBase
{
    /// <summary>Text shown inside the badge.</summary>
    [Parameter, EditorRequired] public string Text { get; set; } = "";

    /// <summary>Name of a status registered in the catalog, whose colours the badge adopts.</summary>
    [Parameter] public string? Status { get; set; }

    /// <summary>Background colour override.</summary>
    [Parameter] public CssColor? Background { get; set; }

    /// <summary>Text colour override.</summary>
    [Parameter] public CssColor? Foreground { get; set; }

    /// <summary>Glyph shown before the text, so the status reads without colour.</summary>
    [Parameter] public Icon? Marker { get; set; }

    /// <summary>Additional attributes applied to the badge element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IStatusBadgeCatalog Catalog { get; set; } = default!;

    private StatusBadgeStyle? Registered
        => string.IsNullOrWhiteSpace(Status) ? null : Catalog.Find(Status);

    private Icon? ResolvedMarker
        => Marker ?? Registered?.Marker;

    // Host class and style are merged rather than splatted, so supplying either
    // never drops the badge's own styling.
    private string CssClass
        => AdditionalAttributes.MergeClass("forma-status-badge");

    private string? InlineStyle
    {
        get
        {
            var (background, foreground) = ResolveColours();
            var declarations = new List<string>(3);

            if (background is not null)
            {
                declarations.Add($"--forma-status-badge-background-color: {background};");
            }

            if (foreground is not null)
            {
                declarations.Add($"--forma-status-badge-foreground-color: {foreground};");
            }

            var hostStyle = AdditionalAttributes.HostValue("style");

            if (!string.IsNullOrWhiteSpace(hostStyle))
            {
                declarations.Add(hostStyle);
            }

            return declarations.Count == 0 ? null : string.Join(' ', declarations);
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() => ArgumentException.ThrowIfNullOrWhiteSpace(Text);

    private (string? Background, string? Foreground) ResolveColours()
    {
        var registered = Registered;
        var background = Background ?? registered?.Background;
        var foreground = Foreground ?? registered?.Foreground;

        return (background?.ToString(), foreground?.ToString());
    }
}
