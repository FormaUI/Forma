namespace Forma.UI.Blazor.Components.Layout;

using Forma.UI.Design.Icons;

using Microsoft.AspNetCore.Components;

/// <summary>One tab within a <see cref="FormaTabs"/>, supplying both its header and its panel.</summary>
public sealed partial class FormaTab : IDisposable
{
    private readonly string _generatedId = Guid.NewGuid().ToString("N")[..8];

    [CascadingParameter] private FormaTabs Parent { get; set; } = default!;

    /// <summary>The tab header's text.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = "";

    /// <summary>A glyph shown before the title.</summary>
    [Parameter] public Icon? Icon { get; set; }

    /// <summary>The identifier a controlled parent selects this tab by. A generated one stands in while this is unset.</summary>
    [Parameter] public string? TabId { get; set; }

    /// <summary>The tab's panel content.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>The identifier actually in force, whether the caller supplied it or not.</summary>
    internal string EffectiveTabId => TabId ?? _generatedId;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Parent is null)
        {
            throw new InvalidOperationException($"{nameof(FormaTab)} must be placed inside a {nameof(FormaTabs)} component.");
        }

        Parent.RegisterTab(this);
    }

    /// <inheritdoc />
    protected override void OnParametersSet() => ArgumentException.ThrowIfNullOrWhiteSpace(Title);

    /// <inheritdoc />
    public void Dispose() => Parent.UnregisterTab(this);
}
