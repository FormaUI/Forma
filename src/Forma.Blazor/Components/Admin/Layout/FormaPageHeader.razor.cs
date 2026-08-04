namespace Forma.Blazor.Components.Admin.Layout;

using Microsoft.AspNetCore.Components;

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
