namespace Forma.Blazor.Components.Feedback;

using Forma.Framework.Css;
using Forma.Framework.Feedback;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;

/// <summary>A pill-shaped label showing the status of a record.</summary>
public sealed partial class FormaStatusBadge : FormaElementBase
{
    /// <summary>The text shown inside the badge.</summary>
    [Parameter, EditorRequired] public string Text { get; set; } = "";

    /// <summary>The name of a status registered in the catalog, whose colors and marker the badge adopts.</summary>
    [Parameter] public string? Status { get; set; }

    /// <summary>The background color of the badge.</summary>
    [Parameter] public CssColor? Background { get; set; }

    /// <summary>The text color of the badge.</summary>
    [Parameter] public CssColor? Foreground { get; set; }

    /// <summary>The glyph shown before the text.</summary>
    [Parameter] public Icon? Marker { get; set; }

    [Inject] private IStatusBadgeCatalog Catalog { get; set; } = default!;

    private StatusBadgeStyle? Registered
        => string.IsNullOrWhiteSpace(Status) ? null : Catalog.Find(Status);

    private Icon? ResolvedMarker
        => Marker ?? Registered?.Marker;

    /// <inheritdoc />
    protected override void OnParametersSet() => ArgumentException.ThrowIfNullOrWhiteSpace(Text);

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-status-badge");

    /// <inheritdoc />
    protected override void BuildRootStyle(CssStyleBuilder builder)
    {
        var (background, foreground) = ResolveColours();

        builder
            .Add("--forma-status-badge-bg-color", background)
            .Add("--forma-status-badge-fg-color", foreground);
    }

    private (string? Background, string? Foreground) ResolveColours()
    {
        var registered = Registered;
        var background = Background ?? registered?.Background;
        var foreground = Foreground ?? registered?.Foreground;

        return (background?.ToString(), foreground?.ToString());
    }
}
