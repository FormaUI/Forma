using Microsoft.AspNetCore.Components;

namespace Site.UI.Components.Admin.Layout;

public sealed partial class SiteTab : IDisposable
{
    [CascadingParameter] private SiteTabs Parent { get; set; } = default!;

    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter] public SiteIcon? Icon { get; set; }
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    internal string Id { get; } = Guid.NewGuid().ToString("N")[..8];

    protected override void OnInitialized()
    {
        if (Parent is null)
            throw new InvalidOperationException($"{nameof(SiteTab)} must be placed inside a {nameof(SiteTabs)} component.");

        Parent.RegisterTab(this);
    }

    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);
    }

    public void Dispose() => Parent.UnregisterTab(this);
}
