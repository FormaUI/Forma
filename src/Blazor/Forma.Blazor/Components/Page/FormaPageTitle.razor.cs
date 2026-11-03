namespace Forma.Blazor.Components.Page;

using Forma.Blazor.Components.Navigation;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A page's heading, which can carry a breadcrumb trail beneath it and controls beside it.</summary>
public sealed partial class FormaPageTitle : FormaElementBase
{
    /// <summary>The title of the page.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = "";

    /// <summary>The breadcrumbs leading to this page.</summary>
    [Parameter] public IReadOnlyList<BreadcrumbItem<object?>>? Breadcrumbs { get; set; }

    /// <summary>The actions rendered beside the heading.</summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet() => ArgumentException.ThrowIfNullOrWhiteSpace(Title);

    // Only the last crumb, when it has no Url, is the current page; an earlier one without a Url is a category label.
    private List<BreadcrumbItem<object?>>? RenderedBreadcrumbs
    {
        get
        {
            if (Breadcrumbs is not { Count: > 0 })
            {
                return null;
            }

            var lastIndex = Breadcrumbs.Count - 1;

            return [.. Breadcrumbs.Select((crumb, index) => crumb with { IsCurrent = crumb.Url is null && index == lastIndex })];
        }
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-page-title");
}
