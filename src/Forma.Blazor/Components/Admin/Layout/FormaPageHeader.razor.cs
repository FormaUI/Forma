using Microsoft.AspNetCore.Components;

namespace Forma.Blazor.Components.Admin.Layout;

public partial class FormaPageHeader
{
    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter] public IReadOnlyList<BreadcrumbItem>? Breadcrumbs { get; set; }
    [Parameter] public RenderFragment? Actions { get; set; }

    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);
    }
}
