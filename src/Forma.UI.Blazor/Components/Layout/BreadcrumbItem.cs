namespace Forma.UI.Blazor.Components.Layout;

using System.Diagnostics.CodeAnalysis;

[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
[SuppressMessage("Design", "CA1056:URI properties should not be strings")]
public sealed record BreadcrumbItem
{
    public string Label { get; }
    public string? Url { get; }

    public BreadcrumbItem(string label, string? url = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        Label = label;
        Url = url;
    }
}
