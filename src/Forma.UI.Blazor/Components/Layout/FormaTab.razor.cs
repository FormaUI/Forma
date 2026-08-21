namespace Forma.UI.Blazor.Components.Layout;

using Forma.UI.Style.Glyphs;

using Microsoft.AspNetCore.Components;

public sealed partial class FormaTab : IDisposable
{
    private readonly string _generatedId = Guid.NewGuid().ToString("N")[..8];

    [CascadingParameter] private FormaTabs Parent { get; set; } = default!;

    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter] public Glyph? Icon { get; set; }

    /// <summary>The identifier a controlled parent selects this tab by, generated when the caller supplies none.</summary>
    [Parameter] public string? Id { get; set; }

    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    internal string TabId => Id ?? _generatedId;

    protected override void OnInitialized()
    {
        if (Parent is null)
        {
            throw new InvalidOperationException($"{nameof(FormaTab)} must be placed inside a {nameof(FormaTabs)} component.");
        }

        Parent.RegisterTab(this);
    }

    protected override void OnParametersSet() => ArgumentException.ThrowIfNullOrWhiteSpace(Title);

    public void Dispose() => Parent.UnregisterTab(this);
}
