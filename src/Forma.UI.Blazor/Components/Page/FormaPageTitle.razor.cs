namespace Forma.UI.Blazor.Components.Page;

using Forma.UI.Blazor.Components.Navigation;

using Microsoft.AspNetCore.Components;

/// <summary>A page's heading, with an optional breadcrumb trail beneath it and controls beside it.</summary>
public partial class FormaPageTitle
{
    /// <summary>The page's heading.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = "";

    /// <summary>The trail of segments leading to this page.</summary>
    [Parameter] public IReadOnlyList<BreadcrumbItem<object?>>? Breadcrumbs { get; set; }

    /// <summary>Controls rendered beside the heading.</summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet() => ArgumentException.ThrowIfNullOrWhiteSpace(Title);

    // Only the last crumb without a Url counts as the current page; an earlier one without
    // a Url is a non-navigable category label, not a claim about where the trail stands.
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
}
