namespace Forma.Blazor.Components.Page;

using Microsoft.AspNetCore.Components;

/// <summary>The document head a Forma host needs: its character set, viewport, title, base address, Forma's own stylesheets, and the framework's asset and head outlets.</summary>
public sealed partial class FormaPageHead : ComponentBase
{
    /// <summary>The document title.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    /// <summary>The address every relative URL resolves against, for a host mounted below the site root.</summary>
    [Parameter] public string BaseHref { get; set; } = "/";

    /// <summary>Head content rendered after Forma's stylesheets, so a host's own styles win over them.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Whether to preload the runtime assets a WebAssembly render mode needs. Requires the Razor Components hosting services.</summary>
    [Parameter] public bool PreloadAssets { get; set; } = true;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);
        ArgumentException.ThrowIfNullOrWhiteSpace(BaseHref);
    }
}
