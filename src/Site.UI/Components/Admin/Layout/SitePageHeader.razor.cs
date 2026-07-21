using Microsoft.AspNetCore.Components;

namespace Site.UI.Components.Admin.Layout;

public partial class SitePageHeader
{
    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter] public IReadOnlyList<BreadcrumbItem>? Breadcrumbs { get; set; }
    [Parameter] public RenderFragment? Actions { get; set; }

    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);
    }
}
