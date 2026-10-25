namespace Forma.Blazor.Components.Layout;

using Forma.Framework.Css;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;

/// <summary>One tab within a <see cref="FormaTabs"/>, supplying both its header and its panel.</summary>
public sealed partial class FormaTab : FormaElementBase
{
    [CascadingParameter] private FormaTabs Parent { get; set; } = default!;

    /// <summary>The tab's title, shown in its header.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = "";

    /// <summary>The icon shown before the title.</summary>
    [Parameter] public Icon? Icon { get; set; }

    /// <summary>The identifier the parent selects this tab by, where <see langword="null"/> is one generated for the tab.</summary>
    [Parameter] public string? TabId { get; set; }

    /// <summary>Whether the tab's header sits at the far end of the strip, after every leading tab.</summary>
    [Parameter] public bool Trailing { get; set; }

    /// <summary>The tab's panel content.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    // What the parent's strip shows for this tab, as of the last parameters it was given.
    private (string Title, Icon? Icon, bool Trailing)? _header;

    /// <summary>The identifier the tab is selected by.</summary>
    internal string EffectiveTabId => TabId ?? Context.RuntimeId.ToString("N");

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Parent is null)
        {
            throw new InvalidOperationException($"{nameof(FormaTab)} must be placed inside a {nameof(FormaTabs)} component.");
        }

        Parent.RegisterTab(this);
    }

    // The parent draws the strip before its panels, so the tabs' new parameters reach them after it is drawn; a header
    // that changed, such as a title in a newly chosen language, asks the parent to draw the strip again.
    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);

        var header = (Title, Icon, Trailing);
        if (_header is { } shown && !shown.Equals(header))
        {
            Parent.UpdateTab();
        }

        _header = header;
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-tab-panel")
            .Add("active", Parent.IsActive(this));

    /// <inheritdoc />
    protected override ValueTask DisposeAsyncCore()
    {
        Parent.UnregisterTab(this);
        return base.DisposeAsyncCore();
    }
}
