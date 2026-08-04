namespace Forma.Blazor.Components.Admin.Data;

using Forma.Blazor.Abstractions.Components.Data;
using Forma.Web.Css;

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

    /// <summary>Additional attributes applied to the badge element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IStatusBadgeCatalog Catalog { get; set; } = default!;

    // Host class and style are merged rather than splatted, so supplying either
    // never drops the badge's own styling.
    private string CssClass
    {
        get
        {
            var hostClass = HostAttribute("class");
            return string.IsNullOrWhiteSpace(hostClass)
                ? "forma-status-badge"
                : $"forma-status-badge {hostClass}";
        }
    }

    private string? InlineStyle
    {
        get
        {
            var (background, foreground) = ResolveColours();
            var declarations = new List<string>(3);

            if (background is not null)
            {
                declarations.Add($"--forma-status-badge-bg: {background};");
            }

            if (foreground is not null)
            {
                declarations.Add($"--forma-status-badge-fg: {foreground};");
            }

            var hostStyle = HostAttribute("style");

            if (!string.IsNullOrWhiteSpace(hostStyle))
            {
                declarations.Add(hostStyle);
            }

            return declarations.Count == 0 ? null : string.Join(' ', declarations);
        }
    }

    protected override void OnParametersSet() => ArgumentException.ThrowIfNullOrWhiteSpace(Text);

    private (string? Background, string? Foreground) ResolveColours()
    {
        var registered = string.IsNullOrWhiteSpace(Status) ? null : Catalog.Find(Status);
        var background = Background ?? registered?.Background;
        var foreground = Foreground ?? registered?.Foreground;

        return (background?.ToString(), foreground?.ToString());
    }

    private string? HostAttribute(string name)
        => AdditionalAttributes is not null && AdditionalAttributes.TryGetValue(name, out var value)
            ? value?.ToString()
            : null;
}
